using BlazorFeatures.Abstractions;
using BlazorFeatures.Abstractions.Extensions;
using BlazorFeatures.Base;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using static BlazorFeatures.Base.FeatureService;

namespace StarterProject.Client.Features.Identity
{
    public class DeleteUser(IServiceProvider serviceProvider) : IBaseFeature<DeleteUser.Request, EmptyResponse>
    {
        public class Request : IBaseFeatureRequest<EmptyResponse>
        {
            public required string UserId { get; set; }
        }

        private readonly HttpClient HttpClient = serviceProvider.GetRequiredService<HttpClient>();
        private readonly JsonSerializerOptions? JsonOptions = serviceProvider.GetService<IOptions<JsonSerializerOptions>>()?.Value;

        protected const string ApiPath = "/api/identity/deleteUser";

        public async Task<FeatureResponse<EmptyResponse>> HandleClient(Request request, IFeatureContext featureContext, CancellationToken cancellationToken = default)
        {
            var response = await HttpClient!.PostAsJsonAsync(ApiPath, request, cancellationToken);
            return await response.AsFeatureResponse<EmptyResponse>(JsonOptions);
        }

        public virtual Task<FeatureResponse<EmptyResponse>> HandleServer(Request request, IFeatureContext featureContext, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
