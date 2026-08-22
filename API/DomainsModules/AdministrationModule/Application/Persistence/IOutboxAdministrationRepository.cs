namespace AdministrationModule.Application.Persistence;

public interface IOutboxAdministrationRepository
{
    Task<OutboxPayloadSearchResult> Search(
        int page,
        int pageSize,
        bool onlyIncomplete,
        CancellationToken cancellationToken = default
    );

    Task<OutboxPayloadEntry?> Requeue(
        long outboxPayloadId,
        CancellationToken cancellationToken = default
    );
}

public sealed record OutboxPayloadEntry(
    long Id,
    string State,
    int RetryCount,
    string? ErrorMessage,
    string StateMachineId,
    Guid AggregateId,
    uint OrderNumber,
    string EventName,
    DateTime Timestamp,
    string ExecutionInfoJson,
    string EventDataJson
);

public sealed record OutboxPayloadSearchResult(
    List<OutboxPayloadEntry> Items,
    int TotalCount
);
