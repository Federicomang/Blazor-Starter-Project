using BlazorFeatures.Abstractions;
using BlazorFeatures.Base;
using BlazorFeatures.Base.Server;
using ClientEventManager = StarterProject.Client.Features.Private.EventManager;

namespace StarterProject.Features.Private
{
    public class EventManager(IServiceProvider sp) : ClientEventManager(sp), IBaseFeatureEndpoint
    {
        public override async Task<FeatureResponse<FeatureService.EmptyResponse>> HandleServer(Request request, IFeatureContext featureContext, CancellationToken cancellationToken = default)
        {
            return FeatureResponse<FeatureService.EmptyResponse>.AsSuccess(new());
        }

        public static void MapEndpoints(IEndpointRouteBuilder builder)
        {
            
        }
    }
}
