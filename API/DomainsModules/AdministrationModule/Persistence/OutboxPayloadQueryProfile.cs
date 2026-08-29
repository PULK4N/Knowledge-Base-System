using System.Linq.Expressions;
using ActionModule.Persistence;
using ActionModule.Shared.Models;
using AdministrationModule.Application.Persistence;
using EventSourcing.Persistence.Models;

namespace AdministrationModule.Persistence;

/// <summary>
/// Outbox rows keep their event metadata inside the serialized payload, so
/// free text search runs over the stored JSON and the delivery error while
/// filtering and sorting use the mapped delivery columns.
/// </summary>
internal sealed class OutboxPayloadQueryProfile
    : IEntityQueryProfile<
        SerializedPayloadMessage,
        OutboxPayloadSearchFilters,
        OutboxPayloadSortField,
        OutboxPayloadRow
    >
{
    public IQueryable<SerializedPayloadMessage> ApplyFilters(
        IQueryable<SerializedPayloadMessage> query,
        OutboxPayloadSearchFilters filters
    )
    {
        if (filters.OnlyIncomplete)
        {
            query = query.Where(
                message => message.Status != MessageStatus.Sent
            );
        }

        if (!string.IsNullOrWhiteSpace(filters.State))
        {
            if (
                !Enum.TryParse<MessageStatus>(
                    filters.State.Trim(),
                    true,
                    out var state
                )
            )
                return query.Where(message => false);

            query = query.Where(message => message.Status == state);
        }

        if (filters.AggregateId is not null)
        {
            var aggregateId = filters.AggregateId.Value;
            query = query.Where(
                message => message.AggregateId == aggregateId
            );
        }

        return query;
    }

    public IQueryable<SerializedPayloadMessage> ApplySearch(
        IQueryable<SerializedPayloadMessage> query,
        string? search
    )
    {
        if (search is null)
            return query;

        var normalizedSearch = Normalize(search);
        return query.Where(
            message =>
                message.SerializedEventExecutionInfo
                    .ToUpper()
                    .Contains(normalizedSearch)
                || message.SerializedEventData
                    .ToUpper()
                    .Contains(normalizedSearch)
                || (
                    message.Error != null
                    && message.Error.ToUpper().Contains(normalizedSearch)
                )
        );
    }

    public IOrderedQueryable<SerializedPayloadMessage> ApplySort(
        IQueryable<SerializedPayloadMessage> query,
        SortRequest<OutboxPayloadSortField> sort
    ) =>
        (sort.Field, sort.Direction) switch
        {
            (
                OutboxPayloadSortField.Id,
                SortDirection.Ascending
            ) => query.OrderBy(message => message.Id),
            (
                OutboxPayloadSortField.Id,
                SortDirection.Descending
            ) => query.OrderByDescending(message => message.Id),
            (
                OutboxPayloadSortField.State,
                SortDirection.Ascending
            ) => query
                .OrderBy(message => message.Status)
                .ThenBy(message => message.Id),
            (
                OutboxPayloadSortField.State,
                SortDirection.Descending
            ) => query
                .OrderByDescending(message => message.Status)
                .ThenByDescending(message => message.Id),
            (
                OutboxPayloadSortField.RetryCount,
                SortDirection.Ascending
            ) => query
                .OrderBy(message => message.ExecutionAttempts)
                .ThenBy(message => message.Id),
            (
                OutboxPayloadSortField.RetryCount,
                SortDirection.Descending
            ) => query
                .OrderByDescending(message => message.ExecutionAttempts)
                .ThenByDescending(message => message.Id),
            (
                OutboxPayloadSortField.AggregateId,
                SortDirection.Ascending
            ) => query
                .OrderBy(message => message.AggregateId)
                .ThenBy(message => message.Id),
            (
                OutboxPayloadSortField.AggregateId,
                SortDirection.Descending
            ) => query
                .OrderByDescending(message => message.AggregateId)
                .ThenByDescending(message => message.Id),
            _ => throw new ArgumentOutOfRangeException(nameof(sort))
        };

    public Expression<
        Func<SerializedPayloadMessage, OutboxPayloadRow>
    > Projection =>
        message => new OutboxPayloadRow(
            message.Id,
            message.Status,
            message.ExecutionAttempts,
            message.Error,
            message.AggregateId,
            message.SerializedEventExecutionInfo,
            message.SerializedEventData
        );

    internal static string Normalize(string value) =>
        value.Trim().ToUpperInvariant();
}

internal sealed record OutboxPayloadRow(
    long Id,
    MessageStatus Status,
    int RetryCount,
    string? ErrorMessage,
    Guid AggregateId,
    string ExecutionInfoJson,
    string EventDataJson
);
