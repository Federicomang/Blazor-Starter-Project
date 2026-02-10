using Microsoft.AspNetCore.Identity;
using StarterProject.Client.Features.Identity.Models;

namespace StarterProject.Database.Entities
{
    public class User : IdentityUser
    {
        public virtual ICollection<IdentityUserRole<string>> UserRoles { get; set; }
        public virtual ICollection<IdentityRole> Roles { get; set; }

        public void FromUserInfo(UserInfoNoId userInfo)
        {
            UserName = userInfo.Name + "." + userInfo.Surname;
            PhoneNumber = userInfo.PhoneNumber;
            Email = userInfo.Email;
        }
    }
}
