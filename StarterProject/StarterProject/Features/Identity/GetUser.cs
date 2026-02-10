using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using StarterProject.Client.Features;
using StarterProject.Client.Features.Identity.Models;
using StarterProject.Client.Infrastructure;
using StarterProject.Database;
using StarterProject.Database.Entities;
using StarterProject.Extensions;
using StarterProject.Tools;
using System.Linq.Dynamic.Core;
using ClientGetUser = StarterProject.Client.Features.Identity.GetUser;

namespace StarterProject.Features.Identity
{
    public class GetUser(
        IHttpContextAccessor httpContextAccessor,
        IStringLocalizer<GetUser> localizer,
        UserManager<User> userManager,
        ApplicationDbContext dbContext) : ClientGetUser, IBaseFeatureEndpoint
    {
        public override async Task<FeatureResponse<Response>> HandleServer(Request request, CancellationToken cancellationToken = default)
        {
            var httpContext = httpContextAccessor.HttpContext;
            if(httpContext == null)
            {
                return FeatureResponse<Response>.AsFailure();
            }
            else
            {
                FeatureResponse<Response> response;
                int statusCode;

                try
                {
                    var user = await GetUserFunc(httpContext, request, cancellationToken);
                    if(user == null)
                    {
                        response = FeatureResponse<Response>.AsFailure(messages: [localizer["User not found"]]);
                        statusCode = 404;
                    }
                    else
                    {
                        response = FeatureResponse<Response>.AsSuccess(new() { User = user });
                        statusCode = 200;
                    }
                }
                catch(Exception e)
                {
                    response = FeatureResponse<Response>.AsFailure(messages: [e.Message]);
                    statusCode = 500;
                }

                httpContext.SetFeatureApiResponse(Results.Json(response, statusCode: statusCode));
                return response;
            }
        }

        private async Task<UserInfo?> GetUserFunc(HttpContext context, Request request, CancellationToken cancellationToken = default)
        {
            var filter = PredicateBuilder.True<User>();
            var thisUser = context.GetItems().User;
            var thisUserRoles = thisUser == null ? [] : await userManager.GetRolesAsync(thisUser);
            if(!thisUserRoles.Contains(ApplicationRoles.Superadmin))
            {
                filter = filter.And(x => !x.Roles.Any(r => r.Name == ApplicationRoles.Superadmin));
            }
            filter = filter.And(x => x.Id == request.UserId);
            var query = dbContext.Users.Filter(filter, null, ClientModelsExpressions.CreateInfoFromUser);
            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        public static void MapEndpoints(IEndpointRouteBuilder builder)
        {
            var apiPath = HttpTools.BuildServerApi(ApiPath, "{userId}");
            builder.MapGet(apiPath, async (HttpContext context, string userId, [FromServices] IFeatureService featureService) =>
            {
                await featureService.Run(new Request() { UserId = userId });
                await context.ApplyApiFeatureResponse();
            }).RequireAuthorization(policy =>
            {
                policy.RequireAuthenticatedUser();
            });
        }
    }
}
