
using BlazorFeatures.Abstractions;
using BlazorFeatures.Abstractions.Extensions;
using BlazorFeatures.Base;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace StarterProject.Client.Features.Generic
{
    public class ChangeLanguage(IServiceProvider serviceProvider) : IBaseFeature<ChangeLanguage.Request, ChangeLanguage.Response>
    {
        public class Request : IBaseFeatureRequest<Response>
        {
            public required string Culture { get; set; }
        }

        public class Response
        {

        }

        private readonly HttpClient HttpClient = serviceProvider.GetRequiredService<HttpClient>();
        private readonly JsonSerializerOptions? JsonOptions = serviceProvider.GetService<IOptions<JsonSerializerOptions>>()?.Value;

        public const string ApiPath = "/api/generic/changeLanguage";

        public async Task<FeatureResponse<Response>> HandleClient(Request request, IFeatureContext featureContext, CancellationToken cancellationToken = default)
        {
            var response = await HttpClient!.PostAsJsonAsync(ApiPath, request, cancellationToken);
            return await response.AsFeatureResponse<Response>(JsonOptions);
        }

        public virtual Task<FeatureResponse<Response>> HandleServer(Request request, IFeatureContext featureContext, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
