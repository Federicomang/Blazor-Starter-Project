namespace StarterProject.Client.Features
{
    public interface IServerFeatureService
    {
        public Task<FeatureResponse<Response>> HandleServer<Response>(IFeatureHandler<Response> handler, Type requestType, IBaseFeatureRequest<Response> request, CancellationToken cancellationToken = default) where Response : class;
    }
}
