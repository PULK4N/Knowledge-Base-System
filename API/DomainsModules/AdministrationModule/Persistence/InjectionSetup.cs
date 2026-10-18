using AdministrationModule.Application.Persistence;
using AdministrationModule.Application.Persistence.UniqueConstraints;
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
        services.AddScoped<
            IUniqueConstraintRepository,
            UniqueConstraintRepository
        >();

        return services;
    }
}
