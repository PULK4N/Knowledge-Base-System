using EventSourcing.Shared.Interfaces;
using IdentityModule.Application.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityModule.Persistence;

public static class InjectionSetup
{
    public static IServiceCollection RegisterIdentityModulePersistence(this IServiceCollection services)
    {
        services.AddScoped<AccountListRepository>();
        services.AddScoped<IAccountListRepository>(serviceProvider =>
            serviceProvider.GetRequiredService<AccountListRepository>());
        services.AddScoped<IProjector, AccountListProjector>();
        return services;
    }
}
