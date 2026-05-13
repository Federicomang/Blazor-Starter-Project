using System.Security.Claims;

namespace StarterProject.OpenApi
{
    public static class OpenApiDocumentNames
    {
        public const string All = "v1";

        public static readonly OpenApiDocumentDefinition[] Documents =
        [
            new(All, "Tutte le API", "/scalar"),
        ];

        public static bool CanAccess(ClaimsPrincipal user, string? documentName)
        {
            if (string.IsNullOrWhiteSpace(documentName))
            {
                return false;
            }

            if (user.HasClaim("api-doc", "*") || user.HasClaim("api-doc", documentName))
            {
                return true;
            }

            return true;
        }
    }

    public sealed record OpenApiDocumentDefinition(string Name, string Title, string ScalarRoute);
}
