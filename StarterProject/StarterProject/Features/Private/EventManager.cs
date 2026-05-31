using BlazorFeatures.Abstractions;
using ClientEventManager = StarterProject.Client.Features.Private.EventManager;

namespace StarterProject.Features.Private
{
    public class EventManager : ClientEventManager, IBaseFeatureEndpoint
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
