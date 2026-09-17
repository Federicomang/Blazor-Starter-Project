using BlazorFeatures.Abstractions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using StarterProject.Extensions;

namespace StarterProject.Infrastructure
{
    public class FeatureContextEnricher(IJSRuntime jsRuntime, NavigationManager navigationManager) : IFeatureCallerContextEnricher
    {
        public async ValueTask EnrichAsync(FeatureCallerContextBuilder context, CancellationToken cancellationToken = default)
        {
            try
            {
                await jsRuntime.InvokeVoidAsync("window.getDotnetRuntime");
                context.JSRuntime = jsRuntime;
                context.BaseUri = navigationManager.BaseUri;
            }
            catch (Exception) { }
        }
    }
}
