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

        public string FirstName { get; set; }
        public string LastName { get; set; }

        public virtual ICollection<IdentityUserRole<string>> UserRoles { get; set; }
        public virtual ICollection<IdentityRole> Roles { get; set; }

        public static readonly Expression<Func<User, UserInfo>> CreateInfoFromEntity = user =>
                new UserInfo()
                {
                    Id = user.Id,
                    Name = user.FirstName,
                    Surname = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    Email = user.Email!,
                    Roles = user.Roles.Select(x => x.Name!).ToList()
                };

        public void FromUserInfo(UserInfoNoId userInfo)
        {
            UserName = string.IsNullOrWhiteSpace(userInfo.UserName) ? userInfo.Email : userInfo.UserName;
            FirstName = userInfo.Name;
            LastName = userInfo.Surname;
            PhoneNumber = userInfo.PhoneNumber;
            Email = userInfo.Email;
        }

        public static Expression<Func<User, bool>> SearchFilter(string searchString)
        {
            searchString = searchString.ToLower();
            var filter = PredicateBuilder.True<User>();
            filter = filter.And(x => (x.UserName != null && x.UserName.ToLower().Contains(searchString))
                || (x.FirstName != null && x.FirstName.ToLower().Contains(searchString))
                || (x.LastName != null && x.LastName.ToLower().Contains(searchString))
                || (x.Email != null && x.Email.ToLower().Contains(searchString))
                || (x.PhoneNumber != null && x.PhoneNumber.ToLower().Contains(searchString)));
            return filter;
        }
    }
}
