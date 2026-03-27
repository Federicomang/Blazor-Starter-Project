using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using StarterProject.Client.Pages.Identity;
using StarterProject.Database.Entities;
using System.Security.Claims;

namespace StarterProject.Infrastructure
{
    public class CustomAuthStateProvider(
        ILoggerFactory loggerFactory,
        IServiceScopeFactory scopeFactory) : RevalidatingServerAuthenticationStateProvider(loggerFactory)
    {
        protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(1);

        protected override async Task<bool> ValidateAuthenticationStateAsync(
            AuthenticationState authenticationState,
            CancellationToken cancellationToken)
        {
            var user = authenticationState.User;

            if (!user.Identity?.IsAuthenticated ?? true)
                return true;

            using var scope = scopeFactory.CreateScope();

            var enricher = scope.ServiceProvider.GetRequiredService<ClaimsEnricher>();

            var enriched = await enricher.RefreshIdentity(user, true);

            // 🔥 se cambia qualcosa → aggiorna
            if (!AreEqual(user, enriched))
            {
                NotifyAuthenticationStateChanged(
                    Task.FromResult(new AuthenticationState(enriched)));
            }

            return true;
        }

        public async Task<ClaimsPrincipal?> ForceRefreshAsync()
        {
            var state = await GetAuthenticationStateAsync();

            if (!state.User.Identity?.IsAuthenticated ?? true)
                return null;

            using var scope = scopeFactory.CreateScope();
            var enricher = scope.ServiceProvider.GetRequiredService<ClaimsEnricher>();

            var enriched = await enricher.RefreshIdentity(state.User, true);

            NotifyAuthenticationStateChanged(
                Task.FromResult(new AuthenticationState(enriched)));

            return enriched;
        }

        private static bool AreEqual(ClaimsPrincipal a, ClaimsPrincipal b)
        {
            var aClaims = a.Claims.Select(c => (c.Type, c.Value)).ToHashSet();
            var bClaims = b.Claims.Select(c => (c.Type, c.Value)).ToHashSet();

            return aClaims.SetEquals(bClaims);
        }
    }
}
