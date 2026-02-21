namespace StarterProject.Client.Features
{
    public interface IBaseFeature
    {
        public Task<FeatureResponse<object>> HandleClient(object request, CancellationToken cancellationToken = default);

        public Task<FeatureResponse<object>> HandleServer(object request, CancellationToken cancellationToken = default);
    }

    public interface IBaseFeature<Request, Response> : IBaseFeature where Request : class, IBaseFeatureRequest<Response> where Response : class
    {
        public Task<FeatureResponse<Response>> HandleClient(Request request, CancellationToken cancellationToken = default);

        public Task<FeatureResponse<Response>> HandleServer(Request request, CancellationToken cancellationToken = default);

        async Task<FeatureResponse<object>> IBaseFeature.HandleClient(object request, CancellationToken cancellationToken)
        {
            var res = await HandleClient((Request)request, cancellationToken);
            return res.AsGeneric();
        }

        async Task<FeatureResponse<object>> IBaseFeature.HandleServer(object request, CancellationToken cancellationToken)
        {
            var res = await HandleServer((Request)request, cancellationToken);
            return res.AsGeneric();
        }
    }
}
