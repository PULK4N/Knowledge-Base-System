using EventSourcing.Persistence;
using EventSourcing.Shared.Models;
using MemoryModule.Domain.Models;
using MemoryModule.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace PostgreSqlModule;

internal sealed class PostgreSqlMemoryToolCallRepository(
    EventSourcingDbContext dbContext
) : IMemoryToolCallRepository
{
    public async Task<List<MemoryToolCall>> Get(
        AggregateId memoryAggregateId,
        CancellationToken cancellationToken = default
    )
    {
        var entries = await dbContext.Set<MemoryToolCallEntry>()
            .AsNoTracking()
            .Where(
                toolCall =>
                    toolCall.MemoryAggregateId == memoryAggregateId.Value
            )
            .OrderBy(toolCall => toolCall.Timestamp)
            .ThenBy(toolCall => toolCall.PromptId)
            .ThenBy(toolCall => toolCall.ToolCallIndex)
            .ToListAsync(cancellationToken);

        return entries.Select(ToReadModel).ToList();
    }

    public async Task Write(
        IReadOnlyCollection<AggregateId> memoryAggregateIds,
        IReadOnlyCollection<MemoryToolCall> toolCalls,
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

        await dbContext.Set<MemoryToolCallEntry>()
            .Where(
                toolCall =>
                    aggregateIds.Contains(toolCall.MemoryAggregateId)
            )
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.Set<MemoryToolCallEntry>().AddRangeAsync(
            toolCalls.Select(ToEntry),
            cancellationToken
        );
        await dbContext.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }

    private static MemoryToolCallEntry ToEntry(MemoryToolCall toolCall) =>
        new()
        {
            MemoryAggregateId = toolCall.MemoryAggregateId.Value,
            PromptId = toolCall.PromptId.Value,
            ToolCallIndex = toolCall.ToolCallIndex,
            ThreadId = toolCall.ThreadId.Value,
            Timestamp = toolCall.Timestamp,
            ToolName = toolCall.ToolName,
            ToolUseId = toolCall.ToolUseId,
            Description = toolCall.Description,
            PayloadJson = toolCall.PayloadJson
        };

    private static MemoryToolCall ToReadModel(MemoryToolCallEntry toolCall) =>
        new(
            AggregateId.FromDatabaseGuid(toolCall.MemoryAggregateId),
            new ThreadId(toolCall.ThreadId),
            new PromptId(toolCall.PromptId),
            toolCall.ToolCallIndex,
            toolCall.Timestamp,
            toolCall.ToolName,
            toolCall.ToolUseId,
            toolCall.Description,
            toolCall.PayloadJson
        );
}
