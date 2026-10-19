using ActionModule.Shared.Models;
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

    public async Task<List<MemorySummary>> GetMany(
        IReadOnlyCollection<AggregateId> memoryAggregateIds,
        CancellationToken cancellationToken = default
    )
    {
        var ids = memoryAggregateIds
            .Select(aggregateId => aggregateId.Value)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
            return [];

        return (await dbContext.Set<MemorySummaryEntry>()
                .AsNoTracking()
                .Where(summary => ids.Contains(summary.MemoryAggregateId))
                .ToListAsync(cancellationToken))
            .Select(ToReadModel)
            .ToList();
    }

    public async Task<MemorySummarySearchResult> Search(
        EntityQuery<MemorySummaryFilters, MemorySummarySortField> request,
        CancellationToken cancellationToken = default
    )
    {
        var query = dbContext.Set<MemorySummaryEntry>().AsNoTracking();

        if (request.NormalizedSearch is { } search)
        {
            var normalizedSearch = search.ToLowerInvariant();
            var matchesThreadId = Guid.TryParse(search, out var threadId);
            var matchesMemoryId = Guid.TryParse(search, out var memoryId);
            query = query.Where(
                summary =>
                    summary.Summary.ToLower().Contains(normalizedSearch)
                    || summary.SessionTitle.ToLower().Contains(normalizedSearch)
                    || (matchesThreadId && summary.ThreadId == threadId)
                    || (matchesMemoryId
                        && summary.MemoryAggregateId == memoryId)
            );
        }

        if (request.Filters.HasSummary is { } hasSummary)
        {
            query = query.Where(
                summary => (summary.Summary.Trim() != string.Empty) == hasSummary
            );
        }

        if (request.Filters.MinimumPromptCount is { } minimumPromptCount)
        {
            query = query.Where(
                summary => summary.PromptCount >= minimumPromptCount
            );
        }

        if (request.Filters.ActiveFrom is { } activeFrom)
        {
            query = query.Where(
                summary => summary.LastActivityTimestamp >= activeFrom
            );
        }

        if (request.Filters.Model is { } model)
        {
            query = FilterByModel(query, model);
        }

        if (request.Filters.ActiveTo is { } activeTo)
        {
            query = query.Where(
                summary =>
                    (summary.FirstPromptTimestamp
                        ?? summary.LastActivityTimestamp) <= activeTo
            );
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var sortedQuery = ApplySort(query, request.Sort);
        var entries = await sortedQuery
            .ThenBy(summary => summary.MemoryAggregateId)
            .Skip(request.Page.Offset)
            .Take(request.Page.Size)
            .ToListAsync(cancellationToken);

        return new MemorySummarySearchResult(
            entries.Select(ToReadModel).ToList(),
            totalCount
        );
    }

    private static IOrderedQueryable<MemorySummaryEntry> ApplySort(
        IQueryable<MemorySummaryEntry> query,
        SortRequest<MemorySummarySortField> sort
    ) =>
        (sort.Field, sort.Direction) switch
        {
            (MemorySummarySortField.LastActivity, SortDirection.Ascending) =>
                query.OrderBy(summary => summary.LastActivityTimestamp),
            (MemorySummarySortField.LastActivity, SortDirection.Descending) =>
                query.OrderByDescending(
                    summary => summary.LastActivityTimestamp
                ),
            (MemorySummarySortField.PromptCount, SortDirection.Ascending) =>
                query.OrderBy(summary => summary.PromptCount),
            (MemorySummarySortField.PromptCount, SortDirection.Descending) =>
                query.OrderByDescending(summary => summary.PromptCount),
            (MemorySummarySortField.FirstPrompt, SortDirection.Ascending) =>
                query.OrderBy(summary => summary.FirstPromptTimestamp),
            (MemorySummarySortField.FirstPrompt, SortDirection.Descending) =>
                query.OrderByDescending(summary => summary.FirstPromptTimestamp),
            (MemorySummarySortField.LastPrompt, SortDirection.Ascending) =>
                query.OrderBy(summary => summary.LastPromptTimestamp),
            (MemorySummarySortField.LastPrompt, SortDirection.Descending) =>
                query.OrderByDescending(summary => summary.LastPromptTimestamp),
            (MemorySummarySortField.SummaryUpdated, SortDirection.Ascending) =>
                query.OrderBy(summary => summary.SummaryTimestamp),
            (MemorySummarySortField.SummaryUpdated, SortDirection.Descending) =>
                query.OrderByDescending(summary => summary.SummaryTimestamp),
            _ => throw new ArgumentOutOfRangeException(nameof(sort))
        };

    public Task<List<string>> GetModels(
        CancellationToken cancellationToken = default
    ) =>
        CreateModelsQuery().ToListAsync(cancellationToken);

    // EF Core cannot translate SelectMany over a text[] column, so the models
    // are unnested in SQL. SqlQueryRaw reads a scalar from a column named Value.
    internal IQueryable<string> CreateModelsQuery() =>
        dbContext.Database.SqlQueryRaw<string>(
            """
            SELECT model AS "Value"
            FROM "MemorySummaryEntries", unnest("Models") AS model
            GROUP BY model
            ORDER BY model
            """
        );

    internal static IQueryable<MemorySummaryEntry> FilterByModel(
        IQueryable<MemorySummaryEntry> query,
        string model
    ) =>
        query.Where(summary => summary.Models.Contains(model));

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
            SessionTitle = summary.SessionTitle,
            Summary = summary.Summary,
            PromptCount = summary.PromptCount,
            FirstPromptTimestamp = summary.FirstPromptTimestamp,
            LastPromptTimestamp = summary.LastPromptTimestamp,
            SummaryTimestamp = summary.SummaryTimestamp,
            LastActivityTimestamp = summary.LastActivityTimestamp,
            Models = summary.Models.ToList()
        };

    private static MemorySummary ToReadModel(MemorySummaryEntry summary) =>
        new(
            AggregateId.FromDatabaseGuid(summary.MemoryAggregateId),
            new ThreadId(summary.ThreadId),
            summary.SessionTitle,
            summary.Summary,
            summary.PromptCount,
            summary.FirstPromptTimestamp,
            summary.LastPromptTimestamp,
            summary.SummaryTimestamp,
            summary.LastActivityTimestamp,
            summary.Models.ToList()
        );
}
