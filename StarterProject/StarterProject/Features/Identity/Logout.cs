using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.JSInterop;
using OpenIddict.Server.AspNetCore;
using StarterProject.Client.Features;
using StarterProject.Extensions;
using ClientLogout = StarterProject.Client.Features.Identity.Logout;
using Response = StarterProject.Client.Features.FeatureService.EmptyResponse;

namespace StarterProject.Features.Identity
{
    public class Logout(IHttpContextAccessor httpContextAccessor, IJSRuntime jsRuntime, NavigationManager navigationManager) : ClientLogout, IBaseFeatureEndpoint
    {
        public override async Task<FeatureResponse<Response>> HandleServer(Request request, CancellationToken cancellationToken = default)
        {
            var httpContext = httpContextAccessor.HttpContext!;
            if(httpContext.IsSocketConnection())
            {
                var jsResponse = await jsRuntime.InvokeAsync<BrowserDoRequest<string>>("window.doRequest", navigationManager.BaseUri.TrimEnd('/') + ApiPath, new
                {
                    method = "POST",
                    headers = new Dictionary<string, string> {
                        { "Content-Type", "application/x-www-form-urlencoded" }
                    },
                    body = ""
                }, false);
                return FeatureResponse<Response>.Create(jsResponse.StatusCode >= 200 && jsResponse.StatusCode < 400, new());
            }
            else if(httpContext.Request.Method == "POST" || httpContext.Request.Method == "GET")
            {
                // Recupera la request OpenID Connect
                var oidRequest = httpContext.GetOpenIddictServerRequest();

                // Redirect post-logout
                var redirectUri = oidRequest?.PostLogoutRedirectUri;

                var authenticationProps = string.IsNullOrEmpty(redirectUri) ? null : new AuthenticationProperties()
                {
                    RedirectUri = redirectUri
                };

                var result = Results.SignOut(authenticationProps, [IdentityConstants.ApplicationScheme, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);

                httpContext.SetFeatureApiResponse(result);
                return FeatureResponse<Response>.AsSuccess(new Response());
            }
            httpContext.SetFeatureApiResponse(Results.BadRequest());
            return FeatureResponse<Response>.AsFailure(null);
        }

        public static void MapEndpoints(IEndpointRouteBuilder builder)
        {
            builder.MapPost(ApiPath, async (HttpContext context, [FromServices] IFeatureService featureService) => {
                await featureService.Run(new Request());
                await context.ApplyApiFeatureResponse();
            });

            builder.MapGet(ApiPath, async (HttpContext context, [FromServices] IFeatureService featureService) => {
                await featureService.Run(new Request());
                await context.ApplyApiFeatureResponse();
            });
        }
    }
}
