namespace SharedModule.DistributedMessaging.Consuming;

/// <summary>
/// One transactional consumer on one queue. Acknowledge commits the delivery,
/// Redeliver rolls it back so the broker retries and eventually dead-letters.
/// </summary>
public interface IQueueSubscription : IDisposable
{
    Task<string?> Receive(TimeSpan timeout, CancellationToken cancellationToken);

    Task Acknowledge(CancellationToken cancellationToken);

    Task Redeliver(CancellationToken cancellationToken);
}

public interface IQueueSubscriptionFactory
{
    Task<IQueueSubscription> Subscribe(
        string queueName, CancellationToken cancellationToken);
}
