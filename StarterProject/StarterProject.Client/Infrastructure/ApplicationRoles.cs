using System.Reflection;

namespace StarterProject.Client.Infrastructure
{
    public class ApplicationRoles
    {
        private static readonly List<string> _allRoles = [];

        public const string Superadmin = "Superadmin";
        public const string Administrator = "Administrator";
        public const string Sales = "Sales";
        public const string ExternalSales = "ExternalSales";

        public static List<string> GetAllRoles()
        {
            lock (_allRoles)
            {
                if (_allRoles.Count == 0)
                {
                    var roles = typeof(ApplicationRoles)
                        .GetFields(BindingFlags.Public | BindingFlags.Static)
                        .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                        .Select(f => (string)f.GetRawConstantValue()!);
                    _allRoles.AddRange(roles);
                }
                return [.. _allRoles];
            }
        }

        internal static List<string> GetSelectableRoles(IEnumerable<string> myRoles)
        {
            if (!myRoles.Any()) return [];
            var roles = GetAllRoles();
            if(!myRoles.Contains(Superadmin)) roles.Remove(Superadmin);
            return roles;
        }
    }
}
