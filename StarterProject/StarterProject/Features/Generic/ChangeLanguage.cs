using BlazorFeatures.Abstractions;
using BlazorFeatures.Base.Server;
using BlazorFeatures.Base.Server.Extensions;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using StarterProject.Client;
using ClientChangeLanguage = StarterProject.Client.Features.Generic.ChangeLanguage;

namespace StarterProject.Features.Generic
{
    public class ChangeLanguage(IServiceProvider sp, IHttpContextAccessor httpContextAccessor) : ClientChangeLanguage(sp), IBaseFeatureEndpoint
    {
        public override async Task<FeatureResponse<Response>> HandleServer(Request request, IFeatureContext featureContext, CancellationToken cancellationToken = default)
        {
            var httpContext = httpContextAccessor.HttpContext;

            if (httpContext != null)
            {
                var requestCulture = new RequestCulture(request.Culture);

                if (ApplicationConstants.SupportedCultures.Any(x => x.Name == requestCulture.Culture.Name))
                {
                    httpContext.Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(request.Culture)),
                    new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddYears(1),
                        IsEssential = true
                    });
                    return FeatureResponse<Response>.AsSuccess(new());
                }
                else
                {
                    return FeatureResponse<Response>.AsFailure(
                        messages: ["The specified culture is not supported."],
                        statusCode: System.Net.HttpStatusCode.BadRequest);
                }
            }

            return FeatureResponse<Response>.AsFailure(messages: ["Unable to change language."]);
        }

        public static void MapEndpoints(IEndpointRouteBuilder builder)
        {
            builder.MapPost(ApiPath, async (HttpContext context, Request request, [FromServices] IFeatureService featureService) =>
            {
                await context.RunFeature(featureService, request);
            });
        }
    }
}
