using StarterProject.Features;
using System.Reflection;

namespace StarterProject.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static void UseFeatureEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
        {
            var endpointRegisterTypes = Assembly.GetCallingAssembly().GetTypes()
                .Where(type => !type.IsAbstract && !type.IsInterface && typeof(IBaseFeatureEndpoint).IsAssignableFrom(type))
                .ToArray();

            foreach (var endpointType in endpointRegisterTypes) {
                endpointType.GetMethod(nameof(IBaseFeatureEndpoint.MapEndpoints))!.Invoke(null, [endpointRouteBuilder]);
            }
        }
    }
}
