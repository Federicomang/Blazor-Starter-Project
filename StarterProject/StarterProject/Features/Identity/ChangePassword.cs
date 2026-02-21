using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StarterProject.Client.Features;
using StarterProject.Database.Entities;
using StarterProject.Extensions;
using ClientChangePassword = StarterProject.Client.Features.Identity.ChangePassword;

namespace StarterProject.Features.Identity
{
    public class ChangePassword(IHttpContextAccessor httpContextAccessor, UserManager<User> userManager) : ClientChangePassword, IBaseFeatureEndpoint
    {
        public override async Task<FeatureResponse<Response>> HandleServer(Request request, CancellationToken cancellationToken = default)
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
            httpContext.SetFeatureApiResponse(Results.Json(response, statusCode: statusCode));
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
