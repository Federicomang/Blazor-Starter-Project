using Hangfire;

namespace StarterProject.Infrastructure.Hangfire
{
    public static class HangfireServicesExtensions
    {
        public static IServiceCollection AddHangfireInFeatures(this IServiceCollection services, string? connectionString)
        {
            // Add Hangfire services.
            services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(connectionString));

            // Add the processing server as IHostedService
            services.AddHangfireServer();

            services.AddSingleton<IHangfireJobContext, HangfireJobContext>();

            GlobalJobFilters.Filters.Add(new HangfireJobContextFilter());

            return services;
        }
    }
}
