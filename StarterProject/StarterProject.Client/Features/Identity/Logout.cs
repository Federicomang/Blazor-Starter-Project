using BlazorFeatures.Abstractions;
using BlazorFeatures.Base;
using System.Text;
using static BlazorFeatures.Base.FeatureService;

namespace StarterProject.Client.Features.Identity
{
    public class Logout(IServiceProvider serviceProvider) : IBaseFeature<Logout.Request, EmptyResponse>
    {
        public record Request() : IBaseFeatureRequest<EmptyResponse>;

        private readonly HttpClient HttpClient = serviceProvider.GetRequiredService<HttpClient>();

        public const string ApiPath = "/api/identity/logout";

        public async Task<FeatureResponse<EmptyResponse>> HandleClient(Request request, IFeatureContext featureContext, CancellationToken cancellationToken = default)
        {
            var content = new StringContent("", Encoding.UTF8, "application/x-www-form-urlencoded");
            var response = await HttpClient!.PostAsync(ApiPath, content, cancellationToken);
            return FeatureResponse<EmptyResponse>.Create(response.IsSuccessStatusCode, new());
        }

        public virtual Task<FeatureResponse<EmptyResponse>> HandleServer(Request request, IFeatureContext featureContext, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
