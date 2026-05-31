using BlazorFeatures.Abstractions;
using BlazorFeatures.Abstractions.Extensions;
using System.Net.Http.Json;
using Response = BlazorFeatures.Abstractions.FeatureService.EmptyResponse;

namespace StarterProject.Client.Features.Identity
{
    public class DeleteUser : IBaseFeature<DeleteUser.Request, Response>
    {
        public class Request : IBaseFeatureRequest<Response>
        {
            public required string UserId { get; set; }
        }

        private readonly HttpClient? HttpClient;

        protected const string ApiPath = "/api/identity/deleteUser";

        protected DeleteUser() { }

        public DeleteUser(HttpClient client)
        {
            HttpClient = client;
        }

        public async Task<FeatureResponse<Response>> HandleClient(Request request, IFeatureContext featureContext, CancellationToken cancellationToken = default)
        {
            var response = await HttpClient!.PostAsJsonAsync(ApiPath, request, cancellationToken);
            return await response.AsFeatureResponse<Response>();
        }

        public virtual Task<FeatureResponse<Response>> HandleServer(Request request, IFeatureContext featureContext, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
