using BlazorFeatures.Abstractions;
using BlazorFeatures.Base.Server;
using BlazorFeatures.Base.Server.Extensions;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Server.AspNetCore;
using StarterProject.Extensions;
using StarterProject.OpenApi;
using ClientLogout = StarterProject.Client.Features.Identity.Logout;
using Response = BlazorFeatures.Base.FeatureService.EmptyResponse;

namespace StarterProject.Features.Identity
{
    public class Logout(IServiceProvider sp) : ClientLogout(sp), IBaseFeatureEndpoint
    {
        public override async Task<FeatureResponse<Response>> HandleServer(Request request, IFeatureContext featureContext, CancellationToken cancellationToken = default)
        {
            if(featureContext is not IHttpFeatureContext httpFeatureContext)
            {
                var callerContext = featureContext.CallerContext;
                var jsResponse = await callerContext.JSRuntime!.DoRequest(callerContext.BaseUri!.TrimEnd('/') + ApiPath, new
                {
                    method = "POST",
                    headers = new Dictionary<string, string> {
                        { "Content-Type", "application/x-www-form-urlencoded" }
                    },
                    body = ""
                });
                return FeatureResponse<Response>.Create(jsResponse.StatusCode >= 200 && jsResponse.StatusCode < 400, new());
            }
            else if(httpFeatureContext.HttpContext.Request.Method == "POST" || httpFeatureContext.HttpContext.Request.Method == "GET")
            {
                // Recupera la request OpenID Connect
                var oidRequest = httpFeatureContext.HttpContext.GetOpenIddictServerRequest();

                // Redirect post-logout
                var redirectUri = oidRequest?.PostLogoutRedirectUri;

                var authenticationProps = string.IsNullOrEmpty(redirectUri) ? null : new AuthenticationProperties()
                {
                    RedirectUri = redirectUri
                };

                var result = Results.SignOut(authenticationProps, [IdentityConstants.ApplicationScheme, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);

                httpFeatureContext.SetHttpResult(request, result);
                return FeatureResponse<Response>.AsSuccess(new Response());
            }
            httpFeatureContext.SetHttpResult(request, Results.BadRequest());
            return FeatureResponse<Response>.AsFailure(statusCode: System.Net.HttpStatusCode.BadRequest);
        }

        public static void MapEndpoints(IEndpointRouteBuilder builder)
        {
            builder.MapPost(ApiPath, async (HttpContext context) => {
                await context.RunFeature(new Request());
            }).WithTags(OpenApiDocumentGroups.Identity);

            builder.MapGet(ApiPath, async (HttpContext context) => {
                await context.RunFeature(new Request());
            }).WithTags(OpenApiDocumentGroups.Identity);
        }
    }
}
