using Microsoft.AspNetCore.Localization;

namespace StarterProject.Infrastructure.Localization
{
    public class HybridRequestCultureProvider : RequestCultureProvider
    {
        private readonly CookieRequestCultureProvider _cookie = new();
        private readonly AcceptLanguageHeaderRequestCultureProvider _accept = new();

        public override async Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext context)
        {
            // Se c'è JWT → usa SOLO Accept-Language
            if (context.Request.Headers.ContainsKey("Authorization"))
            {
                return await _accept.DetermineProviderCultureResult(context);
            }

            // Browser → cookie → fallback accept-language
            var cookieResult = await _cookie.DetermineProviderCultureResult(context);
            if (cookieResult != null)
                return cookieResult;

            return await _accept.DetermineProviderCultureResult(context);
        }
    }
}
