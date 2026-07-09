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
            FeatureResponse<Response> response;
            var httpContext = httpContextAccessor.HttpContext;

            if (httpContext != null)
            {
                int statusCode;
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
                    response = FeatureResponse<Response>.AsSuccess(new());
                    statusCode = StatusCodes.Status200OK;
                }
                else
                {
                    response = FeatureResponse<Response>.AsFailure(messages: ["The specified culture is not supported."]);
                    statusCode = StatusCodes.Status400BadRequest;
                }
                httpContext.SetFeatureApiResponse(Results.Json(response, statusCode: statusCode));
            }
            else
            {
                response = FeatureResponse<Response>.AsFailure(messages: ["Unable to change language."]);
            }

            return response;
        }

        public static void MapEndpoints(IEndpointRouteBuilder builder)
        {
            builder.MapPost(ApiPath, async (HttpContext context, Request request, [FromServices] IFeatureService featureService) =>
            {
                await featureService.Run(request);
                await context.ApplyApiFeatureResponse();
            });
        }
    }
}
