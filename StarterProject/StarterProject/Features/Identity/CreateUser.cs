using BlazorFeatures.Abstractions;
using BlazorFeatures.Base.Server;
using BlazorFeatures.Base.Server.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StarterProject.Client.Infrastructure;
using StarterProject.Database.Entities;
using StarterProject.Features.Identity.Shared;
using StarterProject.OpenApi;
using ClientCreateUser = StarterProject.Client.Features.Identity.CreateUser;

namespace StarterProject.Features.Identity
{
    public class CreateCustomer(
        IServiceProvider sp,
        UserManager<User> userManager,
        IFeatureService featureService) : ClientCreateUser(sp), IBaseFeatureAuthorization, IBaseFeatureEndpoint
    {
        private static void BuildPolicy(AuthorizationPolicyBuilder policy)
        {
            policy.RequireRole(ApplicationRoles.Superadmin, ApplicationRoles.Administrator);
        }

        public override async Task<FeatureResponse<Response>> HandleServer(Request request, IFeatureContext featureContext, CancellationToken cancellationToken = default)
        {
            FeatureResponse<Response> response;
            int statusCode;

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
                    }, featureContext, cancellationToken);
                    response = res.ConvertTo(responseData, null);
                    if (res.Success)
                    {
                        statusCode = StatusCodes.Status200OK;
                    }
                    else
                    {
                        await userManager.DeleteAsync(user);
                        statusCode = (int)(res.StatusCode ?? System.Net.HttpStatusCode.InternalServerError);
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
