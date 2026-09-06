using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedModule.DistributedMessaging.Queues;

namespace SharedModule.DistributedMessaging.Consuming;

public static class Registration
{
    /// <summary>
    /// One worker per provisioned queue. Each needs its own ProvisionedQueue,
    /// which a repeated AddHostedService could not express.
    /// </summary>
    public static IServiceCollection AddStateMachineConsumers(
        this IServiceCollection services,
        List<ProvisionedQueue> queues,
        ConsumerSettings? settings = null
    )
    {
        var consumerSettings = settings ?? new ConsumerSettings();

        foreach (var queue in queues)
            services.AddSingleton<IHostedService>(
                provider => ActivatorUtilities.CreateInstance<QueueWorker>(
                    provider, queue, consumerSettings));

        return services;
    }
}
