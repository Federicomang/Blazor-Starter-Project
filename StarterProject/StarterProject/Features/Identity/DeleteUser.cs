using BlazorFeatures.Abstractions;
using BlazorFeatures.Base.Server;
using BlazorFeatures.Base.Server.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StarterProject.Client.Infrastructure;
using StarterProject.Database.Entities;
using StarterProject.OpenApi;
using ClientDeleteUser = StarterProject.Client.Features.Identity.DeleteUser;
using Response = BlazorFeatures.Base.FeatureService.EmptyResponse;

namespace StarterProject.Features.Identity
{
    public class DeleteUser(IServiceProvider sp, UserManager<User> userManager) : ClientDeleteUser(sp), IBaseFeatureAuthorization, IBaseFeatureEndpoint
    {
        private static void BuildPolicy(AuthorizationPolicyBuilder policy)
        {
            policy.RequireRole(ApplicationRoles.Superadmin, ApplicationRoles.Administrator);
        }

        public override async Task<FeatureResponse<Response>> HandleServer(Request request, IFeatureContext featureContext, CancellationToken cancellationToken = default)
        {
            FeatureResponse<Response> response;
            int statusCode;

            var user = await userManager.FindByIdAsync(request.UserId);

            if(user != null)
            {
                var result = await userManager.DeleteAsync(user);
                if(result.Succeeded)
                {
                    response = FeatureResponse<Response>.AsSuccess(new());
                    statusCode = StatusCodes.Status200OK;
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
