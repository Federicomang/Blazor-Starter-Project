using BlazorFeatures.Base;
using BlazorFeatures.Base.Extensions;
using MudBlazor;
using MudBlazor.Services;
using System.Text.Json;

namespace StarterProject.Client.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddSharedServices(
            this IServiceCollection services,
            Action<FeatureConfigBuilder>? configureFeatures = null)
        {
            services.Configure<JsonSerializerOptions>(options =>
            {
                options.PropertyNameCaseInsensitive = true;
            });
            services.AddFeatures(configureFeatures);
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
