using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using OpenIddict.Abstractions;
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
        IOpenIddictApplicationManager applicationManager,
        RoleManager<IdentityRole> roleManager,
        IMemoryCache cache) : IMiddleware
    {
        private async Task<(bool isApplication, User? user)> GetUserAndPopulateItems(HttpContext httpContext, string identifier)
        {
            var httpContextItems = httpContext.GetItems();
            if (httpContextItems.AuthenticationScheme == IdentityConstants.ApplicationScheme)
            {
                var client = await applicationManager.FindByClientIdAsync(identifier);
                httpContext.Items[HttpContextItems.DATA_PREFIX + nameof(HttpContextItems.ApplicationId)] = client == null ? null : identifier;
                return (true, null);
            }
            else
            {
                var user = await userManager.FindByIdAsync(identifier);
                httpContext.Items[HttpContextItems.DATA_PREFIX + nameof(HttpContextItems.User)] = user;
                return (false, user);
            }
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

            var identifier = identity.FindFirst(Claims.Subject)?.Value;

            if (string.IsNullOrEmpty(identifier))
                return null;

            var (isApplication, user) = await GetUserAndPopulateItems(context, identifier);

            if (isApplication || user == null)
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
