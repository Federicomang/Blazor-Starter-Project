using FluentValidation;
using Microsoft.AspNetCore.Identity;
using StarterProject.Client.Features;
using StarterProject.Client.Infrastructure;
using StarterProject.Database.Entities;
using StarterProject.Extensions;

namespace StarterProject.Features.Identity.Shared
{
    public class AssignRoles(UserManager<User> userManager, IHttpContextAccessor httpContextAccessor) : IBaseFeature<AssignRoles.Request, AssignRoles.Response>
    {
        public class Request : IBaseFeatureRequest<Response>
        {
            public required User User { get; set; }
            public required IEnumerable<string> Roles { get; set; }
            public bool ReplaceExistingRoles { get; set; } = false;
        }

        public class Response
        {
            public int StatusCode { get; set; }
        }

        public class Validator : AbstractValidator<Request>
        {
            public Validator()
            {
                var allRoles = ApplicationRoles.GetAllRoles();
                RuleFor(x => x.User)
                    .NotNull().WithMessage("User cannot be empty");
                RuleFor(x => x.Roles)
                    .NotNull().WithMessage("Roles cannot be null")
                    .Must(x => x.All(r => allRoles.Contains(r))).WithMessage("An invalid role is in the list");
            }
        }

        public Task<FeatureResponse<Response>> HandleClient(Request request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<FeatureResponse<Response>> HandleServer(Request request, CancellationToken cancellationToken = default)
        {
            var thisUser = httpContextAccessor.HttpContext?.GetItems().User;

            if(thisUser == null)
            {
                return FeatureResponse<Response>.AsFailure(new() { StatusCode = StatusCodes.Status401Unauthorized }, messages: ["User not authenticated"]);
            }

            var thisUserRoles = await userManager.GetRolesAsync(thisUser);

            if (request.Roles.Contains(ApplicationRoles.Superadmin))
            {
                if(!thisUserRoles.Contains(ApplicationRoles.Superadmin))
                {
                    return FeatureResponse<Response>.AsFailure(messages: ["An invalid role is in the list"]);
                }
            }

            if (!thisUserRoles.Any(x => x == ApplicationRoles.Superadmin || x == ApplicationRoles.Administrator))
            {
                return FeatureResponse<Response>.AsFailure(messages: ["The user must be an Administrator to apply roles"]);
            }

            var roles = await userManager.GetRolesAsync(request.User);
            var rolesToAdd = request.Roles;

            if (request.ReplaceExistingRoles)
            {
                var removeResult = await userManager.RemoveFromRolesAsync(request.User, roles);
                if(!removeResult.Succeeded)
                {
                    return FeatureResponse<Response>.AsFailure(data: new() { StatusCode = StatusCodes.Status500InternalServerError }, messages: ["An error as occurred"]);
                }
            }
            else
            {
                rolesToAdd = request.Roles.Where(x => !roles.Contains(x));
            }

            var result = rolesToAdd.Any() ? await userManager.AddToRolesAsync(request.User, rolesToAdd) : IdentityResult.Success;

            if(result.Succeeded)
            {
                return FeatureResponse<Response>.AsSuccess(new Response());
            }
            else
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return FeatureResponse<Response>.AsFailure(data: new() { StatusCode = StatusCodes.Status500InternalServerError }, messages: errors);
            }
        }
    }
}
