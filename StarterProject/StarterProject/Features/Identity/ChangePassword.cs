using BlazorFeatures.Abstractions;
using BlazorFeatures.Base.Server;
using BlazorFeatures.Base.Server.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StarterProject.Database.Entities;
using StarterProject.OpenApi;
using ClientChangePassword = StarterProject.Client.Features.Identity.ChangePassword;
using Response = BlazorFeatures.Base.FeatureService.EmptyResponse;

namespace StarterProject.Features.Identity
{
    public class ChangePassword(IServiceProvider sp, IHttpContextAccessor httpContextAccessor, UserManager<User> userManager) : ClientChangePassword(sp), IBaseFeatureAuthorization, IBaseFeatureEndpoint
    {
        private static void BuildPolicy(AuthorizationPolicyBuilder policy)
        {
            policy.RequireAuthenticatedUser();
        }

        public override async Task<FeatureResponse<Response>> HandleServer(Request request, IFeatureContext featureContext, CancellationToken cancellationToken = default)
        {
            FeatureResponse<Response> response;
            int statusCode;
            var httpContext = httpContextAccessor.HttpContext!;
            var userId = userManager.GetUserId(httpContext.User);
            if(string.IsNullOrEmpty(userId))
            {
                response = FeatureResponse<Response>.AsFailure(messages: ["User not authenticated"]);
                statusCode = StatusCodes.Status401Unauthorized;
            }
            else
            {
                var user = await userManager.FindByIdAsync(userId);
                var result = await userManager.ChangePasswordAsync(user!, request.CurrentPassword, request.NewPassword);
                if (result.Succeeded)
                {
                    response = FeatureResponse<Response>.AsSuccess(new());
                    statusCode = StatusCodes.Status200OK;
                }
                else
                {
                    response = FeatureResponse<Response>.AsFailure(messages: result.Errors.Select(x => x.Description));
                    statusCode = StatusCodes.Status400BadRequest;
                }
            }
            return response.WithStatusCode((System.Net.HttpStatusCode)statusCode);
        }

        public static void MapEndpoints(IEndpointRouteBuilder builder)
        {
            builder.MapPost(ApiPath, async (HttpContext context, Request request, [FromServices] IFeatureService featureService) =>
            {
                await context.RunFeature(featureService, request);
            }).RequireAuthorization(BuildPolicy)
                .WithTags(OpenApiDocumentGroups.Identity);
        }

        void IBaseFeatureAuthorization.BuildPolicy(AuthorizationPolicyBuilder policy) => BuildPolicy(policy);
    }
}
