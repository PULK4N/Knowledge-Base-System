using System.Linq.Expressions;
using AdministrationModule.Application.Persistence;
using EventSourcing.Persistence;
using EventSourcing.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace AdministrationModule.Persistence;

public sealed class OutboxAdministrationRepository(
    EventSourcingDbContext dbContext
) : IOutboxAdministrationRepository
{
    public async Task<OutboxPayloadSearchResult> Search(
        int page,
        int pageSize,
        bool onlyIncomplete,
        CancellationToken cancellationToken = default
    )
    {
        IQueryable<SerializedPayloadMessage> messages =
            dbContext.Set<SerializedPayloadMessage>();

        if (onlyIncomplete)
            messages = messages.Where(CreateNotCompletedPredicate());

        var totalCount = await messages.CountAsync(cancellationToken);
        var rows = await messages
            .AsNoTracking()
            .OrderByDescending(message => message.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new OutboxPayloadSearchResult(
            rows.Select(ToEntry).ToList(),
            totalCount
        );
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

        var entry = dbContext.Entry(message);
        var stateProperty = GetStateProperty(entry);
        stateProperty.CurrentValue = stateProperty.Metadata.ClrType.IsEnum
            ? Enum.Parse(stateProperty.Metadata.ClrType, "New")
            : "New";
        var retryCountProperty = GetRetryCountProperty(entry);
        retryCountProperty.CurrentValue = 0;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToEntry(message);
    }

    private OutboxPayloadEntry ToEntry(
        SerializedPayloadMessage message
    )
    {
        var deserialized = message.Deserialize();
        var payload = deserialized.Payload;
        var executionInfo = payload.EventExecutionInfo;
        var entry = dbContext.Entry(message);

        return new OutboxPayloadEntry(
            message.Id,
            GetStateProperty(entry).CurrentValue?.ToString() ?? string.Empty,
            Convert.ToInt32(
                GetRetryCountProperty(entry).CurrentValue
            ),
            GetErrorProperty(entry)?.CurrentValue as string,
            executionInfo.StateMachineId,
            executionInfo.AggregateId.Value,
            executionInfo.OrderNumber,
            executionInfo.EventName,
            executionInfo.Timestamp,
            message.SerializedEventExecutionInfo,
            message.SerializedEventData
        );
    }

    private static PropertyEntry GetStateProperty(
        EntityEntry<SerializedPayloadMessage> entry
    ) =>
        entry.Properties.Single(
            property =>
                property.Metadata.Name.Contains(
                    "State",
                    StringComparison.OrdinalIgnoreCase
                )
                || property.Metadata.Name.Contains(
                    "Status",
                    StringComparison.OrdinalIgnoreCase
                )
        );

    private static PropertyEntry GetRetryCountProperty(
        EntityEntry<SerializedPayloadMessage> entry
    ) =>
        entry.Properties.Single(
            property =>
                IsIntegral(property.Metadata.ClrType)
                && !property.Metadata.IsPrimaryKey()
                && !property.Metadata.IsConcurrencyToken
        );

    private static PropertyEntry? GetErrorProperty(
        EntityEntry<SerializedPayloadMessage> entry
    ) =>
        entry.Properties.SingleOrDefault(
            property => property.Metadata.Name.Contains(
                "Error",
                StringComparison.OrdinalIgnoreCase
            )
        );

    private static bool IsIntegral(Type type)
    {
        var valueType = Nullable.GetUnderlyingType(type) ?? type;

        return valueType == typeof(byte)
            || valueType == typeof(sbyte)
            || valueType == typeof(short)
            || valueType == typeof(ushort)
            || valueType == typeof(int)
            || valueType == typeof(uint)
            || valueType == typeof(long)
            || valueType == typeof(ulong);
    }

    private Expression<Func<SerializedPayloadMessage, bool>>
        CreateNotCompletedPredicate()
    {
        var entityType = dbContext.Model.FindEntityType(
            typeof(SerializedPayloadMessage)
        ) ?? throw new InvalidOperationException(
            "The outbox payload entity is not mapped."
        );
        var stateProperty = entityType.GetProperties().Single(
            property =>
                property.Name.Contains(
                    "State",
                    StringComparison.OrdinalIgnoreCase
                )
                || property.Name.Contains(
                    "Status",
                    StringComparison.OrdinalIgnoreCase
                )
        );
        var completedValue = stateProperty.ClrType.IsEnum
            ? Enum.Parse(stateProperty.ClrType, "Sent")
            : "Sent";
        var parameter = Expression.Parameter(
            typeof(SerializedPayloadMessage),
            "message"
        );
        Expression stateAccess = stateProperty.PropertyInfo is not null
            ? Expression.Property(
                parameter,
                stateProperty.PropertyInfo
            )
            : Expression.Call(
                typeof(EF),
                nameof(EF.Property),
                [stateProperty.ClrType],
                parameter,
                Expression.Constant(stateProperty.Name)
            );

        return Expression.Lambda<Func<SerializedPayloadMessage, bool>>(
            Expression.NotEqual(
                stateAccess,
                Expression.Constant(
                    completedValue,
                    stateProperty.ClrType
                )
            ),
            parameter
        );
    }
}
