using EventSourcing.Shared.Models;
using MemoryModule.Domain.Models;

namespace MemoryModule.Persistence.Interfaces;

public sealed record MemorySummary(
    AggregateId MemoryAggregateId,
    ThreadId ThreadId,
    string Summary,
    int PromptCount,
    DateTime? FirstPromptTimestamp,
    DateTime? LastPromptTimestamp,
    DateTime? SummaryTimestamp,
    DateTime LastActivityTimestamp
);

public sealed record MemorySummarySearchResult(
    List<MemorySummary> Items,
    int TotalCount
);

public interface IMemorySummaryRepository
{
    Task<MemorySummary?> Get(
        AggregateId memoryAggregateId,
        CancellationToken cancellationToken = default
    );

    Task<MemorySummarySearchResult> Search(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default
    );

    Task Write(
        IReadOnlyCollection<AggregateId> memoryAggregateIds,
        IReadOnlyCollection<MemorySummary> summaries,
        CancellationToken cancellationToken = default
    );
}
