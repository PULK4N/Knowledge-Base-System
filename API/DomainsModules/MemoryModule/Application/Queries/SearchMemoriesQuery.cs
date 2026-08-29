using ActionModule.Shared;
using ActionModule.Shared.Models;
using MemoryModule.Application.DTOs;
using MemoryModule.Persistence.Interfaces;

namespace MemoryModule.Application.Queries;

public sealed class SearchMemoriesQuery(
    IMemorySummaryRepository repository
) : PagedQuery<MemorySummaryDto>
{
    public bool? HasSummary { get; set; }
    public int? MinimumPromptCount { get; set; }
    public MemorySummarySortField SortBy { get; set; } =
        MemorySummarySortField.LastActivity;
    public SortDirection SortDirection { get; set; } =
        SortDirection.Descending;

    public override async Task<bool> CanExecute(Executor executor) =>
        await base.CanExecute(executor)
        && MinimumPromptCount is null or >= 1
        && SortBy != MemorySummarySortField.Relevance
        && Enum.IsDefined(SortBy)
        && Enum.IsDefined(SortDirection);

    protected override async Task<PagedResult<MemorySummaryDto>> ExecuteInternal(
        Executor executor
    )
    {
        var result = await repository.Search(
            CreateEntityQuery(
                new MemorySummaryFilters(
                    HasSummary,
                    MinimumPromptCount
                ),
                SortBy,
                SortDirection
            )
        );

        return new PagedResult<MemorySummaryDto>(
            result.Items
                .Select(MemorySummaryDto.FromReadModel)
                .ToList(),
            Page,
            PageSize,
            result.TotalCount
        );
    }
}

public sealed class HybridSearchMemoriesQuery(
    IMemorySearch memorySearch,
    IMemorySummaryRepository summaryRepository
) : PagedQuery<MemorySummaryDto>
{
    // Model-style retrieval is intentionally bounded. Pagination is over the
    // distinct memory sessions found in this ranked hybrid-search window.
    private const int RankedChunkCount = 100;
    private const int CandidateChunkCount = 100;

    public bool? HasSummary { get; set; }
    public int? MinimumPromptCount { get; set; }
    public MemorySummarySortField SortBy { get; set; } =
        MemorySummarySortField.Relevance;
    public SortDirection SortDirection { get; set; } =
        SortDirection.Descending;

    public override async Task<bool> CanExecute(Executor executor) =>
        await base.CanExecute(executor)
        && !string.IsNullOrWhiteSpace(Search)
        && MinimumPromptCount is null or >= 1
        && Enum.IsDefined(SortBy)
        && Enum.IsDefined(SortDirection);

    protected override async Task<PagedResult<MemorySummaryDto>> ExecuteInternal(
        Executor executor
    )
    {
        var rankedDocuments = await memorySearch.Search(
            Search!,
            new HybridMemorySearchOptions
            {
                ResultCount = RankedChunkCount,
                CandidateCount = CandidateChunkCount
            }
        );
        var rankedSessions = rankedDocuments
            .GroupBy(result => result.Memory.MemoryAggregateId)
            .Select(group => group.First())
            .ToList();
        var summaries = await summaryRepository.GetMany(
            rankedSessions
                .Select(result => result.Memory.MemoryAggregateId)
                .ToList()
        );
        var summariesById = summaries.ToDictionary(
            summary => summary.MemoryAggregateId
        );
        var matches = rankedSessions
            .Where(result => summariesById.ContainsKey(
                result.Memory.MemoryAggregateId
            ))
            .Select(
                result => new RankedMemorySummary(
                    summariesById[result.Memory.MemoryAggregateId],
                    result.Score
                )
            )
            .Where(MatchesFilters)
            .ToList();
        var sortedMatches = Sort(matches).ToList();

        return new PagedResult<MemorySummaryDto>(
            sortedMatches
                .Skip((Page - 1) * PageSize)
                .Take(PageSize)
                .Select(match => MemorySummaryDto.FromReadModel(match.Summary))
                .ToList(),
            Page,
            PageSize,
            sortedMatches.Count
        );
    }

    private bool MatchesFilters(RankedMemorySummary match) =>
        (HasSummary is null
            || (!string.IsNullOrWhiteSpace(match.Summary.Summary)
                == HasSummary))
        && (MinimumPromptCount is null
            || match.Summary.PromptCount >= MinimumPromptCount);

    private IOrderedEnumerable<RankedMemorySummary> Sort(
        IEnumerable<RankedMemorySummary> matches
    )
    {
        var ordered = (SortBy, SortDirection) switch
        {
            (MemorySummarySortField.Relevance, SortDirection.Ascending) =>
                matches.OrderBy(match => match.Score),
            (MemorySummarySortField.Relevance, SortDirection.Descending) =>
                matches.OrderByDescending(match => match.Score),
            (MemorySummarySortField.LastActivity, SortDirection.Ascending) =>
                matches.OrderBy(match => match.Summary.LastActivityTimestamp),
            (MemorySummarySortField.LastActivity, SortDirection.Descending) =>
                matches.OrderByDescending(
                    match => match.Summary.LastActivityTimestamp
                ),
            (MemorySummarySortField.PromptCount, SortDirection.Ascending) =>
                matches.OrderBy(match => match.Summary.PromptCount),
            (MemorySummarySortField.PromptCount, SortDirection.Descending) =>
                matches.OrderByDescending(match => match.Summary.PromptCount),
            (MemorySummarySortField.FirstPrompt, SortDirection.Ascending) =>
                matches.OrderBy(match => match.Summary.FirstPromptTimestamp),
            (MemorySummarySortField.FirstPrompt, SortDirection.Descending) =>
                matches.OrderByDescending(
                    match => match.Summary.FirstPromptTimestamp
                ),
            (MemorySummarySortField.LastPrompt, SortDirection.Ascending) =>
                matches.OrderBy(match => match.Summary.LastPromptTimestamp),
            (MemorySummarySortField.LastPrompt, SortDirection.Descending) =>
                matches.OrderByDescending(
                    match => match.Summary.LastPromptTimestamp
                ),
            (MemorySummarySortField.SummaryUpdated, SortDirection.Ascending) =>
                matches.OrderBy(match => match.Summary.SummaryTimestamp),
            (MemorySummarySortField.SummaryUpdated, SortDirection.Descending) =>
                matches.OrderByDescending(
                    match => match.Summary.SummaryTimestamp
                ),
            _ => throw new ArgumentOutOfRangeException(nameof(SortBy))
        };

        return ordered.ThenBy(match => match.Summary.MemoryAggregateId.Value);
    }

    private sealed record RankedMemorySummary(
        MemorySummary Summary,
        double Score
    );
}
