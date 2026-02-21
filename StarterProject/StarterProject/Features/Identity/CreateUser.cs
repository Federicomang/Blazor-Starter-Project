using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StarterProject.Client.Features;
using StarterProject.Client.Infrastructure;
using StarterProject.Database.Entities;
using StarterProject.Extensions;
using StarterProject.Features.Identity.Shared;
using ClientCreateUser = StarterProject.Client.Features.Identity.CreateUser;

namespace StarterProject.Features.Identity
{
    public class CreateCustomer(UserManager<User> userManager, IHttpContextAccessor httpContextAccessor, IFeatureService featureService) : ClientCreateUser, IBaseFeatureEndpoint
    {
        public override async Task<FeatureResponse<Response>> HandleServer(Request request, CancellationToken cancellationToken = default)
        {
            FeatureResponse<Response> response;
            int statusCode;

            var httpContext = httpContextAccessor.HttpContext;

            var user = new User()
            {
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };

            user.FromUserInfo(request.UserInfo);

            var result = await userManager.CreateAsync(user, request.Password);

            if(result.Succeeded)
            {
                var responseData = new Response() { Id = user.Id };
                if(request.UserInfo.Roles.Any())
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
                        statusCode = StatusCodes.Status200OK;
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
