using OpenIddict.EntityFrameworkCore.Models;

namespace StarterProject.Database.Entities.OpenIddict
{
    public class OpenIddictApplication : OpenIddictEntityFrameworkCoreApplication<string, OpenIddictAuthorization, OpenIddictToken>, IAuthEntity
    {
        public const string AuthIdentifier = "Application";

        string IAuthEntity.AuthIdentifier => AuthIdentifier;

        string? IAuthEntity.Id => Id;
    }
}
