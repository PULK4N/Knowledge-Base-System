namespace AdministrationModule.Application.DTOs;

public sealed record ProjectionGroupDto(
    string StateMachineId,
    List<string> ProjectionNames
);

public sealed record ProjectionReplayQueuedResult(
    string Status,
    int QueuedAggregateCount
)
{
    public const string QueuedStatus = "Queued";

    public static ProjectionReplayQueuedResult Queued(
        int queuedAggregateCount
    ) =>
        new(QueuedStatus, queuedAggregateCount);
}

public sealed record ProjectionRunResult(
    string Status,
    int ProcessedAggregateCount
)
{
    public const string CompletedStatus = "Completed";

    public static ProjectionRunResult Completed(
        int processedAggregateCount
    ) =>
        new(CompletedStatus, processedAggregateCount);
}
