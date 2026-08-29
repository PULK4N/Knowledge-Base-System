using ActionModule.Persistence;
using ActionModule.Shared.Models;
using AdministrationModule.Application.Persistence;
using EventSourcing.Persistence;
using EventSourcing.Persistence.Models;
using EventSourcing.Persistence.Serialization;
using EventSourcing.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AdministrationModule.Persistence;

public sealed class OutboxAdministrationRepository(
    EventSourcingDbContext dbContext
) : IOutboxAdministrationRepository
{
    private static readonly OutboxPayloadQueryProfile QueryProfile = new();

    public async Task<PagedResult<OutboxPayloadEntry>> Search(
        EntityQuery<
            OutboxPayloadSearchFilters,
            OutboxPayloadSortField
        > request,
        CancellationToken cancellationToken = default
    )
    {
        var result = await EntityQueryExecutor.Execute(
            dbContext.Set<SerializedPayloadMessage>(),
            request,
            QueryProfile,
            cancellationToken
        );

        return result.Map(ToEntry);
    }

    public async Task<OutboxPayloadEntry?> Requeue(
        long outboxPayloadId,
        CancellationToken cancellationToken = default
    )
    {
        var message = await dbContext
            .Set<SerializedPayloadMessage>()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == outboxPayloadId,
                cancellationToken
            );

        if (message is null)
            return null;

        message.Status = MessageStatus.New;
        message.ExecutionAttempts = 0;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToEntry(ToRow(message));
    }

    /// <summary>
    /// Only the execution info is deserialized so payloads of events that are
    /// no longer registered can still be listed and requeued.
    /// </summary>
    private static OutboxPayloadEntry ToEntry(OutboxPayloadRow row)
    {
        var executionInfo = EventJsonSerializer
            .Deserialize<EventExecutionInfo>(row.ExecutionInfoJson);

        return new OutboxPayloadEntry(
            row.Id,
            row.Status.ToString(),
            row.RetryCount,
            row.ErrorMessage,
            executionInfo.StateMachineId,
            row.AggregateId,
            executionInfo.OrderNumber,
            executionInfo.EventName,
            executionInfo.Timestamp,
            row.ExecutionInfoJson,
            row.EventDataJson
        );
    }

    private static OutboxPayloadRow ToRow(
        SerializedPayloadMessage message
    ) =>
        new(
            message.Id,
            message.Status,
            message.ExecutionAttempts,
            message.Error,
            message.AggregateId,
            message.SerializedEventExecutionInfo,
            message.SerializedEventData
        );
}
