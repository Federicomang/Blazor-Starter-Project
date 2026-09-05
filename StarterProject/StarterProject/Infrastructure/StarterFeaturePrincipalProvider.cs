using BlazorFeatures.Abstractions;
using BlazorFeatures.Base.Server;
using System.Security.Claims;

namespace StarterProject.Infrastructure
{
    public sealed class StarterFeaturePrincipalProvider(
        CustomAuthStateProvider authenticationStateProvider) : IFeaturePrincipalProvider
    {
        public async ValueTask<ClaimsPrincipal> GetPrincipalAsync(
            IFeatureContext featureContext,
            CancellationToken cancellationToken = default)
        {
            if (featureContext is IHttpFeatureContext httpFeatureContext)
                return httpFeatureContext.HttpContext.User;

            return await authenticationStateProvider.ForceRefreshAsync()
                ?? new ClaimsPrincipal(new ClaimsIdentity());
        }
    }
}
