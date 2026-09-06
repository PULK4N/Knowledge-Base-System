namespace SharedModule.DistributedMessaging.Queues;

/// <summary>
/// One queue the broker is configured to hold. The set of these is the only
/// thing publishers may send to and the only thing consumers are started for.
/// </summary>
public sealed record ProvisionedQueue(string StateMachineId, DeliveryRole Role)
{
    public string Name => QueueNames.For(StateMachineId, Role);
}
