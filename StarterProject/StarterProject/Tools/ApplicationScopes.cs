using OpenIddict.Abstractions;
using System.Reflection;

namespace StarterProject.Tools
{
    public class ApplicationScopes
    {
        public const string Api = "api";

        private static IEnumerable<string> InternalGetAllScopes(Type t)
        {
            var constants = t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && !f.IsInitOnly)
                .Select(f => (string)f.GetRawConstantValue()!);

            var nestedConstants = t.GetNestedTypes(BindingFlags.Public)
                .SelectMany(InternalGetAllScopes);

            return constants.Concat(nestedConstants);
        }

        public static string[] GetAllScopes()
        {
            var myScopes = InternalGetAllScopes(typeof(ApplicationScopes));
            var openIddictScopes = InternalGetAllScopes(typeof(OpenIddictConstants.Scopes));
            return [.. myScopes, .. openIddictScopes];
        }
    }
}
