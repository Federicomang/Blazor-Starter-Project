using StarterProject.Client.Extensions;
using StarterProject.Client.Features.Identity.Models;
using StarterProject.Client.Infrastructure;
using StarterProject.Client.Tools;

namespace StarterProject.Client.Features.Identity
{
    public class GetUsers : IBaseFeature<GetUsers.Request, GetUsers.Response>
    {
        public class Request : PagedRequest, IBaseFeatureRequest<Response>
        {
            public bool IncludeCurrentUser { get; set; }

            public string? SearchString { get; set; }
        }

        public class Response
        {
            public required PagedResponse<UserInfo> Users { get; set; }
        }

        private readonly HttpClient? HttpClient;

        protected const string ApiPath = "/api/identity/getUsers";

        protected GetUsers() { }

        public GetUsers(HttpClient client)
        {
            HttpClient = client;
        }

        public async Task<FeatureResponse<Response>> HandleClient(Request request, CancellationToken cancellationToken = default)
        {
            var response = await HttpClient!.GetAsync($"{ApiPath}?{HttpTools.ToUrlEncodedString(request)}", cancellationToken);
            return await response.AsFeatureResponse<Response>();
        }

        public virtual Task<FeatureResponse<Response>> HandleServer(Request request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
