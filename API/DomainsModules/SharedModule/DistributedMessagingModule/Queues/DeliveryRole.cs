namespace SharedModule.DistributedMessaging.Queues;

/// <summary>
/// Separates handlers that may safely run again on a redelivery from handlers
/// that cause external effects, so each kind gets its own queue.
/// </summary>
public enum DeliveryRole
{
    Projections,
    Hooks
}
