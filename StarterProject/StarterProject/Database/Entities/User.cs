using Microsoft.AspNetCore.Identity;
using StarterProject.Client.Features.Identity.Models;
using StarterProject.Tools;
using System.Linq.Expressions;

namespace StarterProject.Database.Entities
{
    public class User : IdentityUser, IAuthEntity
    {
        public const string AuthIdentifier = nameof(User);

        string IAuthEntity.AuthIdentifier => AuthIdentifier;

        string? IAuthEntity.Id => Id;

        public virtual ICollection<IdentityUserRole<string>> UserRoles { get; set; }
        public virtual ICollection<IdentityRole> Roles { get; set; }

        public static readonly Expression<Func<User, UserInfo>> CreateInfoFromEntity = user =>
                new UserInfo()
                {
                    Id = user.Id,
                    Name = user.UserName == null ? "" : user.UserName.Split('.')[0],
                    Surname = user.UserName == null ? "" : (user.UserName.Split('.').Length > 1 ? user.UserName.Split('.')[1] : ""),
                    PhoneNumber = user.PhoneNumber,
                    Email = user.Email!,
                    Roles = user.Roles.Select(x => x.Name!).ToList()
                };

        public void FromUserInfo(UserInfoNoId userInfo)
        {
            UserName = userInfo.Name + "." + userInfo.Surname;
            PhoneNumber = userInfo.PhoneNumber;
            Email = userInfo.Email;
        }

        public static Expression<Func<User, bool>> SearchFilter(string searchString)
        {
            searchString = searchString.ToLower();
            var filter = PredicateBuilder.True<User>();
            filter = filter.And(x => (x.UserName == null ? false : x.UserName.ToLower().Contains(searchString))
                || (x.Email == null ? false : x.Email.ToLower().Contains(searchString))
                || (x.PhoneNumber == null ? false : x.PhoneNumber.ToLower().Contains(searchString)));
            return filter;
        }
    }
}
