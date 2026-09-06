namespace SharedModule.DistributedMessaging.Queues;

public static class QueueNames
{
    public const string Prefix = "knowledge-base";

    public static string For(string stateMachineId, DeliveryRole role)
    {
        if (string.IsNullOrWhiteSpace(stateMachineId))
            throw new ArgumentException(
                "A queue name needs a state machine id.", nameof(stateMachineId));

        return $"{Prefix}.{stateMachineId}.{RoleSegment(role)}";
    }

    public static string DeadLetterFor(string stateMachineId, DeliveryRole role) =>
        $"{For(stateMachineId, role)}.dlq";

    private static string RoleSegment(DeliveryRole role) => role switch
    {
        DeliveryRole.Projections => "projections",
        DeliveryRole.Hooks => "hooks",
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown delivery role.")
    };
}
