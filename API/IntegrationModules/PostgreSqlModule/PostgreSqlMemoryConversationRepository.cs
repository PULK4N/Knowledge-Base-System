using EventSourcing.Persistence;
using EventSourcing.Shared.Models;
using MemoryModule.Domain.Models;
using MemoryModule.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace PostgreSqlModule;

internal sealed class PostgreSqlMemoryConversationRepository(
    EventSourcingDbContext dbContext
) : IMemoryConversationRepository
{
    public async Task<List<MemoryConversationMessage>> Get(
        AggregateId memoryAggregateId,
        CancellationToken cancellationToken = default
    )
    {
        var entries = await dbContext.Set<MemoryConversationEntry>()
            .AsNoTracking()
            .Where(
                message =>
                    message.MemoryAggregateId == memoryAggregateId.Value
            )
            .OrderBy(message => message.Timestamp)
            .ThenBy(message => message.PromptId)
            .ThenBy(message => message.HookIndex)
            .ToListAsync(cancellationToken);

        return entries.Select(ToReadModel).ToList();
    }

    public async Task Write(
        IReadOnlyCollection<AggregateId> memoryAggregateIds,
        IReadOnlyCollection<MemoryConversationMessage> messages,
        CancellationToken cancellationToken = default
    )
    {
        var aggregateIds = memoryAggregateIds
            .Select(aggregateId => aggregateId.Value)
            .Distinct()
            .ToList();

        if (aggregateIds.Count == 0)
            return;

        await using var transaction = dbContext.Database.CurrentTransaction is null
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        await dbContext.Set<MemoryConversationEntry>()
            .Where(
                message =>
                    aggregateIds.Contains(message.MemoryAggregateId)
            )
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.Set<MemoryConversationEntry>().AddRangeAsync(
            messages.Select(ToEntry),
            cancellationToken
        );
        await dbContext.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }

    private static MemoryConversationEntry ToEntry(
        MemoryConversationMessage message
    ) =>
        new()
        {
            MemoryAggregateId = message.MemoryAggregateId.Value,
            PromptId = message.PromptId.Value,
            HookIndex = message.HookIndex,
            ThreadId = message.ThreadId.Value,
            Timestamp = message.Timestamp,
            HookEventName = message.HookEventName,
            Role = message.Role,
            Message = message.Message,
            PayloadJson = message.PayloadJson
        };

    private static MemoryConversationMessage ToReadModel(
        MemoryConversationEntry message
    ) =>
        new(
            AggregateId.FromDatabaseGuid(message.MemoryAggregateId),
            new ThreadId(message.ThreadId),
            new PromptId(message.PromptId),
            message.HookIndex,
            message.Timestamp,
            message.HookEventName,
            message.Role,
            message.Message,
            message.PayloadJson
        );
}
