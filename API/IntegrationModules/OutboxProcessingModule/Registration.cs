using Microsoft.Extensions.DependencyInjection;
using OutboxProcessingModule.Application;

namespace OutboxProcessingModule;

public static class Registration
{
    public static IServiceCollection RegisterOutboxProcessing(
        this IServiceCollection services
    )
    {
        services.AddScoped<ProjectorRegistry>();
        services.AddScoped<ProjectionSelector>();
        services.AddScoped<IOutboxQueueResolver, OutboxQueueResolver>();

        return services;
    }
}
