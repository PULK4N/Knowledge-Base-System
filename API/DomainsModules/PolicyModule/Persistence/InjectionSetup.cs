using EventSourcing.Shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using PolicyModule.Persistence.Interfaces;

namespace PolicyModule.Persistence;

public static class InjectionSetup
{
    public static IServiceCollection RegisterPolicyModulePersistence(
        this IServiceCollection services
    )
    {
        services.AddScoped<PolicyTextRepository>();
        services.AddScoped<IPolicyTextRepository>(
            serviceProvider =>
                serviceProvider.GetRequiredService<PolicyTextRepository>()
        );
        services.AddScoped<IProjector, GeneralPolicyTextProjector>();
        services.AddScoped<IProjector, TopicPolicyTextProjector>();
        services.AddScoped<IProjector, ProjectPolicyTextProjector>();
        services.AddScoped<IProjector, ProjectTopicProjector>();

        return services;
    }
}
