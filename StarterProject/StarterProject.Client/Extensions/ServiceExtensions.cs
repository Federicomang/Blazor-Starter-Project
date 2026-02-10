using MudBlazor;
using MudBlazor.Services;
using StarterProject.Client.Attributes;
using StarterProject.Client.Features;
using System.Reflection;

namespace StarterProject.Client.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddFeatures(this IServiceCollection services, params Assembly[] assemblies)
        {
            if(assemblies.Length == 0)
            {
                assemblies = [Assembly.GetCallingAssembly()];
            }

            services.AddScoped<IFeatureService, FeatureService>();

            var baseFeatureType = typeof(IBaseFeature<,>);

            var featureTypes = assemblies.SelectMany(assembly =>
                assembly.GetTypes().Where(type => !type.IsAbstract && !type.IsInterface)
                    .Select(type =>
                    {
                        var interfaces = type.GetInterfaces();
                        List<Type> allInterfaces = [];
                        var otherTypes = type.GetCustomAttribute<FeatureOtherImplementationAttribute>()?.Types ?? [];
                        var serviceLifetime = type.GetCustomAttribute<FeatureServiceLifetimeAttribute>()?.Lifetime ?? ServiceLifetime.Scoped;
                        var isFeature = false;
                        foreach(var i in interfaces)
                        {
                            if(i.IsGenericType && i.GetGenericTypeDefinition() == baseFeatureType)
                            {
                                isFeature = true;
                                allInterfaces.Add(i);
                            }
                            else if(otherTypes.Contains(i))
                            {
                                allInterfaces.Add(i);
                            }
                        }
                        if(!isFeature)
                        {
                            allInterfaces.Clear();
                        }
                        return (Interfaces: allInterfaces, Implementation: type, Lifetime: serviceLifetime);
                    })
            );

            foreach(var (Interfaces, Implementation, Lifetime) in featureTypes)
            {
                if(Interfaces.Count > 0)
                {
                    services.Add(ServiceDescriptor.Describe(Implementation, Implementation, Lifetime));
                    foreach (var i in Interfaces)
                    {
                        services.Add(ServiceDescriptor.Describe(i, sp => sp.GetRequiredService(Implementation), Lifetime));
                    }
                }
            }

            return services;
        }

        public static IServiceCollection AddSharedServices(this IServiceCollection services)
        {
            services.AddFeatures(Assembly.GetCallingAssembly());
            services.AddMudServices(options =>
            {
                options.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
            });
            services.AddLocalization(options =>
            {
                options.ResourcesPath = "Resources";
            });
            return services;
        }
    }
}
