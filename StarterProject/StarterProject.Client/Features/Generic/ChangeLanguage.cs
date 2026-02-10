
using StarterProject.Client.Extensions;
using System.Net.Http.Json;

namespace StarterProject.Client.Features.Generic
{
    public class ChangeLanguage : IBaseFeature<ChangeLanguage.Request, ChangeLanguage.Response>
    {
        public class Request : IBaseFeatureRequest<Response>
        {
            public required string Culture { get; set; }
        }

        public class Response
        {

        }

        private readonly HttpClient? HttpClient;

        public const string ApiPath = "/api/generic/changeLanguage";

        protected ChangeLanguage() { }

        public ChangeLanguage(HttpClient client)
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
