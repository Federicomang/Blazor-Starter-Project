using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StarterProject.Client.Infrastructure;
using StarterProject.Database;
using StarterProject.Database.Entities;

namespace StarterProject.Extensions
{
    public static class DbExtensions
    {
        public static async Task InitDb(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();

            using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            context.Database.Migrate();

            using (var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>())
            {
                var allRoles = ApplicationRoles.GetAllRoles();

                var dbRoles = await context.Roles.Select(x => x.Name).ToListAsync();

                foreach (var role in allRoles)
                {
                    if (!dbRoles.Contains(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole(role));
                    }
                }
            }

            const string superadminMail = "federicomangini10@gmail.com";

            using var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var exists = await context.Users.AnyAsync(x => x.Email == superadminMail);

            if (!exists)
            {
                var user = new User()
                {
                    UserName = superadminMail,
                    Email = superadminMail,
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(user, "Test123!");
                await userManager.AddToRoleAsync(user, ApplicationRoles.Superadmin);
            }
        }
    }
}
