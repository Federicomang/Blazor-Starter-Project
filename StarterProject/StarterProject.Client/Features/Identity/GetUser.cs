using BlazorFeatures.Abstractions;
using BlazorFeatures.Abstractions.Extensions;
using BlazorFeatures.Base;
using Microsoft.Extensions.Options;
using StarterProject.Client.Features.Identity.Models;
using System.Text.Json;

namespace StarterProject.Client.Features.Identity
{
    public class GetUser(IServiceProvider serviceProvider) : IBaseFeature<GetUser.Request, GetUser.Response>
    {
        public class Request : IBaseFeatureRequest<Response>
        {
            public required string UserId { get; set; }
        }

        public class Response
        {
            public required UserInfo User { get; set; }
        }

        private readonly HttpClient HttpClient = serviceProvider.GetRequiredService<HttpClient>();
        private readonly JsonSerializerOptions? JsonOptions = serviceProvider.GetService<IOptions<JsonSerializerOptions>>()?.Value;

        protected const string ApiPath = "/api/identity/getUser/{0}";

        public async Task<FeatureResponse<Response>> HandleClient(Request request, IFeatureContext featureContext, CancellationToken cancellationToken = default)
        {
            var response = await HttpClient!.GetAsync(string.Format(ApiPath, request.UserId), cancellationToken);
            return await response.AsFeatureResponse<Response>(JsonOptions);
        }

        public virtual Task<FeatureResponse<Response>> HandleServer(Request request, IFeatureContext featureContext, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
