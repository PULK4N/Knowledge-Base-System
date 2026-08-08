using EventSourcing.Shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using SkillsModule.Persistence.Interfaces;

namespace SkillsModule.Persistence;

public static class InjectionSetup
{
    public static IServiceCollection RegisterSkillsModulePersistence(
        this IServiceCollection services
    )
    {
        services.AddScoped<SkillSummaryRepository>();
        services.AddScoped<ISkillSummaryRepository>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    SkillSummaryRepository
                >()
        );
        services.AddScoped<IProjector, SkillSummaryProjector>();
        services.AddScoped<ISkillSearch, SkillSearch>();
        services.AddScoped<IProjector, SkillSearchProjector>();

        return services;
    }
}
