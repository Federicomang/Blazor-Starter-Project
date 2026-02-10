using Microsoft.AspNetCore.Localization;

namespace StarterProject.Middlewares
{
    public class HybridRequestLocalizationMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            var cookieName = CookieRequestCultureProvider.DefaultCookieName;

            var isJwt = context.Request.Headers.ContainsKey("Authorization");

            if (!isJwt && !context.Request.Cookies.ContainsKey(cookieName))
            {
                var culture = context.Features.Get<IRequestCultureFeature>()?.RequestCulture;

                if (culture != null)
                {
                    context.Response.Cookies.Append(
                        cookieName,
                        CookieRequestCultureProvider.MakeCookieValue(culture),
                        new CookieOptions
                        {
                            Expires = DateTimeOffset.UtcNow.AddYears(1),
                            IsEssential = true
                        });
                }
            }

            await next(context);
        }
    }
}
