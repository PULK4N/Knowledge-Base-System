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

    /// <summary>
    /// Returns every row that is not Sent to New with a zero retry count and
    /// reports how many rows changed. Includes rows a publisher may hold as
    /// Reading: that publisher's completion then fails on the row version and
    /// the row is published again, which consumers tolerate by design.
    /// </summary>
    Task<int> RequeueIncomplete(
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
