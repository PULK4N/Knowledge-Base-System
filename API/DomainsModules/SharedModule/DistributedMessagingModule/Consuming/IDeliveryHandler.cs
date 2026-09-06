using SharedModule.DistributedMessaging.Queues;

namespace SharedModule.DistributedMessaging.Consuming;

/// <summary>
/// Runs one delivered message. A handler serves exactly one role, so a
/// projections consumer can never run hooks and the other way round.
/// </summary>
public interface IDeliveryHandler
{
    DeliveryRole Role { get; }

    Task Handle(string body, CancellationToken cancellationToken);
}
