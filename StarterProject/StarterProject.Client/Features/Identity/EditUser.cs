using StarterProject.Client.Extensions;
using StarterProject.Client.Features.Identity.Models;
using System.Net.Http.Json;

namespace StarterProject.Client.Features.Identity
{
    public class EditUser : IBaseFeature<EditUser.Request, EditUser.Response>
    {
        public class Request : IBaseFeatureRequest<Response>
        {
            public required UserInfo UserInfo { get; set; }

            public string RolesStr => string.Join(',', UserInfo);
        }

        public class Response
        {
            public required UserInfo UserInfo { get; set; }
        }

        private readonly HttpClient? HttpClient;

        protected const string ApiPath = "/api/identity/editUser";

        protected EditUser() { }

        public EditUser(HttpClient client)
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
