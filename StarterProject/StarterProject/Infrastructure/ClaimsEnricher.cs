using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using OpenIddict.Abstractions;
using StarterProject.Database.Entities;
using StarterProject.Database.Entities.OpenIddict;
using StarterProject.Tools;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static StarterProject.Extensions.HttpContextExtensions;

namespace StarterProject.Infrastructure
{
    public class ClaimsEnricher(
        IHttpContextAccessor httpContextAccessor,
        UserManager<User> userManager,
        IOpenIddictApplicationManager applicationManager,
        RoleManager<IdentityRole> roleManager,
        IMemoryCache cache)
    {
        private async Task GetUserAndPopulateItems(HttpContext? httpContext, InternalCheck result, string tokenType, string identifier)
        {
            if (tokenType == GrantTypes.ClientCredentials)
            {
                var client = (OpenIddictApplication?)await applicationManager.FindByClientIdAsync(identifier);
                httpContext?.Items[HttpContextItems.DATA_PREFIX + nameof(HttpContextItems.Application)] = client;
                result.Claims.Add(new Claim(CustomClaims.AuthIdentifier, OpenIddictApplication.AuthIdentifier));
            }
            else
            {
                var user = await userManager.FindByIdAsync(identifier);
                httpContext?.Items[HttpContextItems.DATA_PREFIX + nameof(HttpContextItems.User)] = user;
                result.User = user;
                result.Claims.Add(new Claim(CustomClaims.AuthIdentifier, User.AuthIdentifier));
                result.AddIdentityClaims = tokenType == GrantTypes.Password;
            }
        }

        private class InternalCheck
        {
            public bool AddIdentityClaims { get; set; } = false;

            public User? User { get; set; }

            public ClaimsIdentity? Identity { get; set; }

            public List<Claim> Claims { get; set; } = [];
        }

        private async Task<InternalCheck> Check(HttpContext? context, ClaimsPrincipal principal)
        {
            var result = new InternalCheck();

            if (principal.Identity?.IsAuthenticated != true)
                return result;

            if (principal.Identity is not ClaimsIdentity identity)
                return result;

            var identifier = identity.FindFirst(Claims.Subject)?.Value;

            if (string.IsNullOrEmpty(identifier))
                return result;

            var tokenType = identity.FindFirst(CustomClaims.OidGrantType)?.Value;

            if (string.IsNullOrEmpty(tokenType))
                return result;

            result.Identity = identity;
            await GetUserAndPopulateItems(context, result, tokenType, identifier);
            return result;
        }

        public async Task<ClaimsPrincipal> RefreshIdentity(ClaimsPrincipal principal, bool newPrincipal = false)
        {
            var result = await Check(httpContextAccessor.HttpContext, principal);

            if (result.AddIdentityClaims)
            {
                // Cache key
                var cacheKey = $"claims_{result.User!.Id}_{result.User.SecurityStamp}";

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

                if(newPrincipal)
                {
                    var oldIdentity = result.Identity!;
                    var newIdentity = new ClaimsIdentity(oldIdentity.AuthenticationType);

                    foreach (var claim in oldIdentity.Claims)
                        newIdentity.AddClaim(claim);

                    result.Identity = newIdentity;
                }

                foreach (var claim in cached!)
                {
                    if (!result.Identity!.HasClaim(claim.Type, claim.Value))
                        result.Identity.AddClaim(claim);
                }
            }

            foreach (var claim in result.Claims)
            {
                result.Identity!.AddClaim(claim);
            }

            return newPrincipal ? new ClaimsPrincipal(result.Identity!) : principal;
        }
    }
}
