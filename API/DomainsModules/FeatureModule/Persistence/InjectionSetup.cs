using EventSourcing.Shared.Interfaces;
using FeatureModule.Persistence.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FeatureModule.Persistence;

public static class InjectionSetup
{
    public static IServiceCollection RegisterFeatureModulePersistence(
        this IServiceCollection services
    )
    {
        services.AddScoped<FeatureSummaryRepository>();
        services.AddScoped<IFeatureSummaryRepository>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    FeatureSummaryRepository
                >()
        );
        services.AddScoped<IProjector, FeatureSummaryProjector>();
        services.AddScoped<FeatureSearchRepository>();
        services.AddScoped<IFeatureSearchRepository>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    FeatureSearchRepository
                >()
        );
        services.AddScoped<IProjector, FeatureSearchProjector>();
        services.AddScoped<
            IFeatureResearchSearch,
            FeatureResearchSearch
        >();
        services.AddScoped<IProjector, FeatureResearchSearchProjector>();

        return services;
    }
}
