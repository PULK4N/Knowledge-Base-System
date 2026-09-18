using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OutboxProcessingModule.Application;
using OutboxProcessingModule.Hosting;
using OutboxProcessingModule.Persistence;
using SharedModule.DistributedMessaging.Consuming;
using SharedModule.DistributedMessaging.Projections;

namespace OutboxProcessingModule;

public static class Registration
{
    public static IServiceCollection RegisterOutboxProcessing(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddOptions<OutboxProcessingOptions>()
            .Bind(configuration.GetSection(OutboxProcessingOptions.SectionName));

        services.AddScoped<ProjectorRegistry>();
        services.AddMemoryCache();
        services.TryAddSingleton<IProjectionCheckpointCache, LocalProjectionCheckpointCache>();
        services.AddScoped<ProjectionSelector>();
        services.AddScoped<IOutboxQueueResolver, OutboxQueueResolver>();
        services.AddScoped<IOutboxDispatchRepository, OutboxDispatchRepository>();
        services.AddScoped<OutboxDispatchStep>();

        // One way to connect for publisher and consumers alike.
        services.AddSingleton<BrokerConnectionFactory>();

        // The connection outlives every cycle; the publisher only borrows it.
        services.AddSingleton<NmsConnectionManager>();
        services.AddScoped<IOutboxPublisher, TransactionalOutboxPublisher>();

        services.AddHostedService<OutboxPublisherWorker>();

        services.AddSingleton<ProvisionedQueueProvider>();
        services.AddSingleton<IQueueSubscriptionFactory, NmsQueueSubscriptionFactory>();
        services.AddScoped<IDeliveryHandler, ProjectionDeliveryHandler>();

        return services;
    }
}
