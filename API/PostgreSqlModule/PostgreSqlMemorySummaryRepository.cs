using EventSourcing.Persistence;
using EventSourcing.Shared.Models;
using MemoryModule.Domain.Models;
using MemoryModule.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace PostgreSqlModule;

internal sealed class PostgreSqlMemorySummaryRepository(
    EventSourcingDbContext dbContext
) : IMemorySummaryRepository
{
    public async Task<MemorySummary?> Get(
        AggregateId memoryAggregateId,
        CancellationToken cancellationToken = default
    )
    {
        var entry = await dbContext.Set<MemorySummaryEntry>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                summary =>
                    summary.MemoryAggregateId == memoryAggregateId.Value,
                cancellationToken
            );

        return entry is null ? null : ToReadModel(entry);
    }

    public async Task<MemorySummarySearchResult> Search(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default
    )
    {
        var query = dbContext.Set<MemorySummaryEntry>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLowerInvariant();
            var matchesThreadId = Guid.TryParse(search, out var threadId);
            query = query.Where(
                summary =>
                    summary.Summary.ToLower().Contains(normalizedSearch)
                    || (matchesThreadId && summary.ThreadId == threadId)
            );
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var entries = await query
            .OrderByDescending(summary => summary.LastActivityTimestamp)
            .ThenBy(summary => summary.MemoryAggregateId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new MemorySummarySearchResult(
            entries.Select(ToReadModel).ToList(),
            totalCount
        );
    }

    public async Task Write(
        IReadOnlyCollection<AggregateId> memoryAggregateIds,
        IReadOnlyCollection<MemorySummary> summaries,
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

        await dbContext.Set<MemorySummaryEntry>()
            .Where(summary => aggregateIds.Contains(summary.MemoryAggregateId))
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.Set<MemorySummaryEntry>().AddRangeAsync(
            summaries.Select(ToEntry),
            cancellationToken
        );
        await dbContext.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }

    private static MemorySummaryEntry ToEntry(MemorySummary summary) =>
        new()
        {
            MemoryAggregateId = summary.MemoryAggregateId.Value,
            ThreadId = summary.ThreadId.Value,
            Summary = summary.Summary,
            PromptCount = summary.PromptCount,
            FirstPromptTimestamp = summary.FirstPromptTimestamp,
            LastPromptTimestamp = summary.LastPromptTimestamp,
            SummaryTimestamp = summary.SummaryTimestamp,
            LastActivityTimestamp = summary.LastActivityTimestamp
        };

    private static MemorySummary ToReadModel(MemorySummaryEntry summary) =>
        new(
            AggregateId.FromDatabaseGuid(summary.MemoryAggregateId),
            new ThreadId(summary.ThreadId),
            summary.Summary,
            summary.PromptCount,
            summary.FirstPromptTimestamp,
            summary.LastPromptTimestamp,
            summary.SummaryTimestamp,
            summary.LastActivityTimestamp
        );
}
