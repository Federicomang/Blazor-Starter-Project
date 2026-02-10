using StarterProject.Client.Extensions;
using StarterProject.Client.Features.Identity.Models;
using System.Net.Http.Json;

namespace StarterProject.Client.Features.Identity
{
    public class CreateUser : IBaseFeature<CreateUser.Request, CreateUser.Response>
    {
        public class Request : IBaseFeatureRequest<Response>
        {
            public UserInfoNoId UserInfo { get; set; } = UserInfoNoId.Empty;
            public string Password { get; set; }

            public string? RolesStr => UserInfo == null ? null : string.Join(',', UserInfo.Roles);
        }

        public class Response
        {
            public required string Id { get; set; }
        }

        private readonly HttpClient? HttpClient;

        protected const string ApiPath = "/api/identity/createUser";

        protected CreateUser() { }

        public CreateUser(HttpClient client)
        {
            HttpClient = client;
        }

        public async Task<FeatureResponse<Response>> HandleClient(Request request, CancellationToken cancellationToken = default)
        {
            var response = await HttpClient!.PostAsJsonAsync(ApiPath, request, cancellationToken);
            return await response.AsFeatureResponse<Response>();
        }

        public virtual Task<FeatureResponse<Response>> HandleServer(Request request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
