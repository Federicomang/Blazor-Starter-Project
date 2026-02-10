using StarterProject.Client.Features;

namespace StarterProject.Client.Extensions
{
    public static class HttpCilentExtensions
    {
        public static async Task<FeatureResponse<T>> AsFeatureResponse<T>(this HttpResponseMessage response, Func<string?, Task<FeatureResponse<T>>>? customDeserialize = null) where T : class
        {
            return await FeatureResponse<T>.FromHttpResponse(response, customDeserialize);
        }
    }
}
