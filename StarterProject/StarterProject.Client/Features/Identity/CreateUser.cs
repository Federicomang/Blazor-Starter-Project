using BlazorFeatures.Abstractions;
using BlazorFeatures.Abstractions.Extensions;
using BlazorFeatures.Base;
using Microsoft.Extensions.Options;
using StarterProject.Client.Features.Identity.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace StarterProject.Client.Features.Identity
{
    public class CreateUser(IServiceProvider serviceProvider) : IBaseFeature<CreateUser.Request, CreateUser.Response>
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

        private readonly HttpClient HttpClient = serviceProvider.GetRequiredService<HttpClient>();
        private readonly JsonSerializerOptions? JsonOptions = serviceProvider.GetService<IOptions<JsonSerializerOptions>>()?.Value;

        protected const string ApiPath = "/api/identity/createUser";

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
