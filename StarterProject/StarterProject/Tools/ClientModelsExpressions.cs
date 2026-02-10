using StarterProject.Client.Features.Identity.Models;
using StarterProject.Database.Entities;
using System.Linq.Expressions;

namespace StarterProject.Tools
{
    public static class ClientModelsExpressions
    {
        public static readonly Expression<Func<User, UserInfo>> CreateInfoFromUser = user =>
                new UserInfo()
                {
                    Id = user.Id,
                    Name = user.UserName == null ? "" : user.UserName.Split('.')[0],
                    Surname = user.UserName == null ? "" : (user.UserName.Split('.').Length > 1 ? user.UserName.Split('.')[1] : ""),
                    PhoneNumber = user.PhoneNumber,
                    Email = user.Email!,
                    Roles = user.Roles.Select(x => x.Name!)
                };
    }
}
