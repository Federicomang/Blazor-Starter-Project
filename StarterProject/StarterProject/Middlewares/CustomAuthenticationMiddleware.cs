using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using StarterProject.Database.Entities;
using StarterProject.Extensions;
using StarterProject.Tools;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static StarterProject.Extensions.HttpContextExtensions;

namespace StarterProject.Middlewares
{
    public class CustomAuthenticationMiddleware(
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager,
        IMemoryCache cache) : IMiddleware
    {
        private async Task<User?> GetUser(HttpContext httpContext, string userId)
        {
            var user = await userManager.FindByIdAsync(userId);
            httpContext.Items[HttpContextItems.DATA_PREFIX + nameof(HttpContextItems.User)] = user;
            return user;
        }

        private class InternalCheck
        {
            public required User User { get; set; }

            public required ClaimsIdentity Identity { get; set; }
        }

        private async Task<InternalCheck?> Check(HttpContext context, ClaimsPrincipal principal)
        {
            if (principal.Identity?.IsAuthenticated != true)
                return null;

            if (principal.Identity is not ClaimsIdentity identity)
                return null;

            var userId = identity.FindFirst(Claims.Subject)?.Value;

            if (string.IsNullOrEmpty(userId))
                return null;

            var user = await GetUser(context, userId);
            if (user == null)
                return null;

            var httpContextItems = context.GetItems();
            if (httpContextItems.AuthenticationScheme == IdentityConstants.ApplicationScheme)
                return null;

            if (!identity.HasClaim(CustomClaims.OidGrantType, GrantTypes.Password))
                return null;

            return new()
            {
                User = user,
                Identity = identity
            };
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var principal = context.User;

            var result = await Check(context, principal);

            if (result != null)
            {
                // Cache key
                var cacheKey = $"claims_{result.User.Id}_{result.User.SecurityStamp}";

                // Get from cache or create
                var cached = await cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

                    var roles = await userManager.GetRolesAsync(result.User);

                    var roleClaims = new List<Claim>();

                    foreach (var roleName in roles)
                    {
                        roleClaims.Add(new Claim(Claims.Role, roleName));

                        var role = await roleManager.FindByNameAsync(roleName);
                        if (role != null)
                        {
                            var claims = await roleManager.GetClaimsAsync(role);
                            roleClaims.AddRange(claims);
                        }
                    }

                    return roleClaims;
                });

                foreach (var claim in cached!)
                {
                    if (!result.Identity.HasClaim(claim.Type, claim.Value))
                        result.Identity.AddClaim(claim);
                }
            }

            await next(context);
        }
    }
}
