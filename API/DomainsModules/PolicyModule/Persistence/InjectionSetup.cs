using EventSourcing.Shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using PolicyModule.Interfaces;

namespace PolicyModule.Persistence;

public static class InjectionSetup
{
    public static IServiceCollection RegisterPolicyModulePersistence(
        this IServiceCollection services
    )
    {
        services.AddScoped<PolicyTextRepository>();
        services.AddScoped<ISessionPolicyTextService>(
            serviceProvider =>
                serviceProvider.GetRequiredService<PolicyTextRepository>()
        );
        services.AddScoped<IProjector, GeneralPolicyTextProjector>();
        services.AddScoped<IProjector, TopicPolicyTextProjector>();
        services.AddScoped<IProjector, AgentFamilyPolicyTextProjector>();

        return services;
    }
}
