using AdministrationModule.Application.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace AdministrationModule.Persistence;

public static class InjectionSetup
{
    public static IServiceCollection RegisterAdministrationModulePersistence(
        this IServiceCollection services
    )
    {
        services.AddScoped<
            IProjectionReplayRepository,
            ProjectionReplayRepository
        >();
        services.AddScoped<
            IOutboxAdministrationRepository,
            OutboxAdministrationRepository
        >();

        return services;
    }
}
