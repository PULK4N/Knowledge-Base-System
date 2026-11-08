using EventSourcing.Shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using SettingsModule.Application.Persistence;

namespace SettingsModule.Persistence;

public static class InjectionSetup
{
    public static IServiceCollection RegisterSettingsModulePersistence(this IServiceCollection services)
    {
        services.AddScoped<CodeReviewSettingsRepository>();
        services.AddScoped<ICodeReviewSettingsReader>(provider => provider.GetRequiredService<CodeReviewSettingsRepository>());
        services.AddScoped<IProjector, CodeReviewSettingsProjector>();
        return services;
    }
}
