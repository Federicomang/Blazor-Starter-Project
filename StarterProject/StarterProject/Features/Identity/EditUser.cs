using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarterProject.Client.Features;
using StarterProject.Client.Infrastructure;
using StarterProject.Database;
using StarterProject.Database.Entities;
using StarterProject.Extensions;
using StarterProject.Features.Identity.Shared;
using StarterProject.Tools;
using ClientEditUser = StarterProject.Client.Features.Identity.EditUser;

namespace StarterProject.Features.Identity
{
    public class EditUser(UserManager<User> userManager, ApplicationDbContext dbContext, IHttpContextAccessor httpContextAccessor, IFeatureService featureService) : ClientEditUser, IBaseFeatureEndpoint
    {
        public override async Task<FeatureResponse<Response>> HandleServer(Request request, CancellationToken cancellationToken = default)
        {
            FeatureResponse<Response> response;
            int statusCode;

            var httpContext = httpContextAccessor.HttpContext;

            var user = await userManager.FindByIdAsync(request.UserInfo.Id);

            if(user != null)
            {
                user.FromUserInfo(request.UserInfo);
                var result = await userManager.UpdateAsync(user);
                if(result.Succeeded)
                {
                    var userInfo = await dbContext.Users.Where(x => x.Id == user.Id)
                        .Select(ClientModelsExpressions.CreateInfoFromUser)
                        .FirstOrDefaultAsync(cancellationToken);
                    var responseData = new Response()
                    {
                        UserInfo = userInfo!
                    };
                    if (request.UserInfo.Roles.Any())
                    {
                        var res = await featureService.Run(new AssignRoles.Request()
                        {
                            User = user,
                            Roles = request.UserInfo.Roles,
                            ReplaceExistingRoles = true
                        }, cancellationToken);
                        response = res.ConvertTo(responseData, null);
                        if (res.Success)
                        {
                            statusCode = 200;
                        }
                        else
                        {
                            await userManager.DeleteAsync(user);
                            statusCode = res.Data == null ? StatusCodes.Status400BadRequest : res.Data.StatusCode;
                        }

                    }
                    else
                    {
                        response = FeatureResponse<Response>.AsSuccess(responseData);
                        statusCode = StatusCodes.Status200OK;
                    }
                }
                else
                {
                    response = FeatureResponse<Response>.AsFailure(messages: result.Errors.Select(x => x.Description));
                    statusCode = StatusCodes.Status500InternalServerError;
                }
            }
            else
            {
                response = FeatureResponse<Response>.AsFailure(messages: ["User not found"]);
                statusCode = StatusCodes.Status400BadRequest;
            }

            httpContext?.SetFeatureApiResponse(Results.Json(response, statusCode: statusCode));

            return response;
        }

        public static void MapEndpoints(IEndpointRouteBuilder builder)
        {
            builder.MapPost(ApiPath, async (HttpContext context, Request request, [FromServices] IFeatureService featureService) =>
            {
                await featureService.Run(request);
                await context.ApplyApiFeatureResponse();
            }).RequireAuthorization(policy =>
            {
                policy.RequireRole(ApplicationRoles.Superadmin, ApplicationRoles.Administrator);
            });
        }
    }
}
