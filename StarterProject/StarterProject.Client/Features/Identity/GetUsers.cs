using BlazorFeatures.Abstractions;
using BlazorFeatures.Abstractions.Extensions;
using BlazorFeatures.Abstractions.Tools;
using BlazorFeatures.Base;
using Microsoft.Extensions.Options;
using StarterProject.Client.Features.Identity.Models;
using System.Text.Json;

namespace StarterProject.Client.Features.Identity
{
    public class GetUsers(IServiceProvider serviceProvider) : IBaseFeature<GetUsers.Request, GetUsers.Response>
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

        private readonly HttpClient HttpClient = serviceProvider.GetRequiredService<HttpClient>();
        private readonly JsonSerializerOptions? JsonOptions = serviceProvider.GetService<IOptions<JsonSerializerOptions>>()?.Value;

        protected const string ApiPath = "/api/identity/getUsers";

        public async Task<FeatureResponse<Response>> HandleClient(Request request, IFeatureContext featureContext, CancellationToken cancellationToken = default)
        {
            var response = await HttpClient!.GetAsync($"{ApiPath}?{HttpTools.ToUrlEncodedString(request)}", cancellationToken);
            return await response.AsFeatureResponse<Response>(JsonOptions);
        }

        public virtual Task<FeatureResponse<Response>> HandleServer(Request request, IFeatureContext featureContext, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
