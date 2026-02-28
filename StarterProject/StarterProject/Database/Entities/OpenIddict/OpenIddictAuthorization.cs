using OpenIddict.EntityFrameworkCore.Models;

namespace StarterProject.Database.Entities.OpenIddict
{
    public class OpenIddictAuthorization : OpenIddictEntityFrameworkCoreAuthorization<string, OpenIddictApplication, OpenIddictToken>
    {
    }
}
