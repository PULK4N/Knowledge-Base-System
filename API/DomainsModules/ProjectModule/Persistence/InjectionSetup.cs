using EventSourcing.Shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using ProjectModule.Persistence.Interfaces;

namespace ProjectModule.Persistence;

public static class InjectionSetup
{
    public static IServiceCollection RegisterProjectModulePersistence(
        this IServiceCollection services
    )
    {
        services.AddScoped<ProjectPolicyTextRepository>();
        services.AddScoped<IProjectPolicyTextRepository>(
            serviceProvider =>
                serviceProvider.GetRequiredService<ProjectPolicyTextRepository>()
        );
        services.AddScoped<PolicyProjectSummaryRepository>();
        services.AddScoped<IPolicyProjectSummaryRepository>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    PolicyProjectSummaryRepository
                >()
        );
        services.AddScoped<IProjector, ProjectPolicyTextProjector>();
        services.AddScoped<IProjector, ProjectTopicProjector>();
        services.AddScoped<IProjector, PolicyProjectSummaryProjector>();

        return services;
    }
}
