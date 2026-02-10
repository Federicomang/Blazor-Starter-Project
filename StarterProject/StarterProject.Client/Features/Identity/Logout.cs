using System.Text;
using Response = StarterProject.Client.Features.FeatureService.EmptyResponse;

namespace StarterProject.Client.Features.Identity
{
    public class Logout : IBaseFeature<Logout.Request, Response>
    {
        public record Request() : IBaseFeatureRequest<Response>;

        private readonly HttpClient? HttpClient;

        protected Logout() { }

        public const string ApiPath = "/api/identity/logout";

        public Logout(HttpClient client)
        {
            HttpClient = client;
        }

        public async Task<FeatureResponse<Response>> HandleClient(Request request, CancellationToken cancellationToken = default)
        {
            var content = new StringContent("", Encoding.UTF8, "application/x-www-form-urlencoded");
            var response = await HttpClient!.PostAsync(ApiPath, content, cancellationToken);
            return FeatureResponse<Response>.Create(response.IsSuccessStatusCode, new());
        }

        public virtual Task<FeatureResponse<Response>> HandleServer(Request request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
