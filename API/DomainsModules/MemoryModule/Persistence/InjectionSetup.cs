using EventSourcing.Shared.Interfaces;
using MemoryModule.Persistence.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace MemoryModule.Persistence;

public static class InjectionSetup
{
    public static IServiceCollection RegisterMemoryModulePersistence(
        this IServiceCollection services
    )
    {
        services.AddScoped<IMemorySearch, MemorySearch>();
        services.AddScoped<IProjector, MemorySearchProjector>();
        services.AddScoped<IProjector, MemorySummaryProjector>();

        return services;
    }
}
