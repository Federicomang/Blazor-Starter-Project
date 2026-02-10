using StarterProject.Client.Extensions;
using StarterProject.Client.Features.Identity.Models;

namespace StarterProject.Client.Features.Identity
{
    public class GetUser : IBaseFeature<GetUser.Request, GetUser.Response>
    {
        public class Request : IBaseFeatureRequest<Response>
        {
            public required string UserId { get; set; }
        }

        public class Response
        {
            public required UserInfo User { get; set; }
        }

        private readonly HttpClient? HttpClient;

        protected const string ApiPath = "/api/identity/getUser/{0}";

        protected GetUser() { }

        public GetUser(HttpClient client)
        {
            HttpClient = client;
        }

        public async Task<FeatureResponse<Response>> HandleClient(Request request, CancellationToken cancellationToken = default)
        {
            var response = await HttpClient!.GetAsync(string.Format(ApiPath, request.UserId), cancellationToken);
            return await response.AsFeatureResponse<Response>();
        }

        public virtual Task<FeatureResponse<Response>> HandleServer(Request request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
