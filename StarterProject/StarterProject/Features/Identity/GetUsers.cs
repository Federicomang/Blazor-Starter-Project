using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StarterProject.Client.Features;
using StarterProject.Client.Features.Identity.Models;
using StarterProject.Client.Infrastructure;
using StarterProject.Database;
using StarterProject.Database.Entities;
using StarterProject.Extensions;
using StarterProject.Tools;
using System.Linq.Dynamic.Core;
using ClientGetUsers = StarterProject.Client.Features.Identity.GetUsers;

namespace StarterProject.Features.Identity
{
    public class GetUsers(
        IHttpContextAccessor httpContextAccessor,
        UserManager<User> userManager,
        ApplicationDbContext dbContext) : ClientGetUsers, IBaseFeatureEndpoint
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
                    var users = await GetUserList(httpContext, request, cancellationToken);
                    response = FeatureResponse<Response>.AsSuccess(new() { Users = users });
                    statusCode = 200;
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

        private async Task<PagedResponse<UserInfo>> GetUserList(HttpContext context, Request request, CancellationToken cancellationToken = default)
        {
            var filter = PredicateBuilder.True<User>();
            var thisUser = context.GetItems().User;
            var thisUserRoles = thisUser == null ? [] : await userManager.GetRolesAsync(thisUser);
            if(!thisUserRoles.Contains(ApplicationRoles.Superadmin))
            {
                filter = filter.And(x => !x.Roles.Any(r => r.Name == ApplicationRoles.Superadmin));
            }
            if(!request.IncludeCurrentUser && thisUser != null)
            {
                filter = filter.And(x => x.Id != thisUser.Id);
            }
            if(!string.IsNullOrEmpty(request.SearchString))
            {
                filter = filter.And(User.SearchFilter(request.SearchString));
            }
            var query = dbContext.Users.Filter(filter, request.OrderBy, User.CreateInfoFromEntity);
            return await query.ToPaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);
        }

        public static void MapEndpoints(IEndpointRouteBuilder builder)
        {
            builder.MapGet(ApiPath, async (HttpContext context, [AsParameters] Request request, [FromServices] IFeatureService featureService) =>
            {
                await featureService.Run(request);
                await context.ApplyApiFeatureResponse();
            }).RequireAuthorization(policy =>
            {
                policy.RequireAuthenticatedUser();
            });
        }
    }
}
