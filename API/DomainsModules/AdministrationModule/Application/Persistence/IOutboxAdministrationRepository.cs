using ActionModule.Shared.Models;

namespace AdministrationModule.Application.Persistence;

public interface IOutboxAdministrationRepository
{
    Task<PagedResult<OutboxPayloadEntry>> Search(
        EntityQuery<
            OutboxPayloadSearchFilters,
            OutboxPayloadSortField
        > request,
        CancellationToken cancellationToken = default
    );

    Task<OutboxPayloadEntry?> Requeue(
        long outboxPayloadId,
        CancellationToken cancellationToken = default
    );
}

/// <summary>
/// Delivery state is filtered by its name so the application layer stays
/// independent of the event sourcing message status type.
/// </summary>
public sealed record OutboxPayloadSearchFilters(
    bool OnlyIncomplete,
    string? State,
    Guid? AggregateId
);

public enum OutboxPayloadSortField
{
    Id,
    State,
    RetryCount,
    AggregateId
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
