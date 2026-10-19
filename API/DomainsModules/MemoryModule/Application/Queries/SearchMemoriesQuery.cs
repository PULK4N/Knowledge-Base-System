using ActionModule.Shared;
using ActionModule.Shared.Models;
using EmbeddingModule;
using MemoryModule.Application.DTOs;
using MemoryModule.Persistence.Interfaces;

namespace MemoryModule.Application.Queries;

public sealed class SearchMemoriesQuery(
    IMemorySummaryRepository repository
) : PagedQuery<MemorySummaryDto>
{
    public bool? HasSummary { get; set; }
    public int? MinimumPromptCount { get; set; }
    public DateTime? ActiveFrom { get; set; }
    public DateTime? ActiveTo { get; set; }
    public string? Model { get; set; }
    public MemorySummarySortField SortBy { get; set; } =
        MemorySummarySortField.LastActivity;
    public SortDirection SortDirection { get; set; } =
        SortDirection.Descending;

    public override async Task<bool> CanExecute(Executor executor) =>
        await base.CanExecute(executor)
        && MinimumPromptCount is null or >= 1
        && MemoryActivityRange.IsValid(ActiveFrom, ActiveTo)
        && MemoryModelFilter.IsValid(Model)
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
                    MinimumPromptCount,
                    MemoryActivityRange.ToUtc(ActiveFrom),
                    MemoryActivityRange.ToUtc(ActiveTo),
                    MemoryModelFilter.Normalize(Model)
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
    private const int DistinctSessionCount = 50;

    public bool? HasSummary { get; set; }
    public int? MinimumPromptCount { get; set; }
    public DateTime? ActiveFrom { get; set; }
    public DateTime? ActiveTo { get; set; }

    /// <summary>
    /// Words the full-text leg requires. Search is the semantic query. Either
    /// one may be empty, and that leg is then skipped.
    /// </summary>
    public List<string> Keywords { get; set; } = [];

    /// <summary>
    /// Keeps only prompt chunks recorded with this model.
    /// </summary>
    public string? Model { get; set; }

    public MemorySummarySortField SortBy { get; set; } =
        MemorySummarySortField.Relevance;
    public SortDirection SortDirection { get; set; } =
        SortDirection.Descending;

    public override async Task<bool> CanExecute(Executor executor) =>
        await base.CanExecute(executor)
        && (!string.IsNullOrWhiteSpace(Search) || Keywords.Count > 0)
        && (Keywords.Count == 0 || SearchKeywordLimits.AreValid(Keywords))
        && MinimumPromptCount is null or >= 1
        && MemoryActivityRange.IsValid(ActiveFrom, ActiveTo)
        && MemoryModelFilter.IsValid(Model)
        && Enum.IsDefined(SortBy)
        && Enum.IsDefined(SortDirection);

    protected override async Task<PagedResult<MemorySummaryDto>> ExecuteInternal(
        Executor executor
    )
    {
        var rankedDocuments = await memorySearch.SearchWithSources(
            Search,
            Keywords,
            new HybridMemorySearchOptions
            {
                ResultCount = RankedChunkCount,
                CandidateCount = CandidateChunkCount,
                SourceResultCount = DistinctSessionCount,
                Model = MemoryModelFilter.Normalize(Model)
            }
        );
        // The ranked window can be filled by one session, so the
        // session-grouped results contribute the sessions it crowded out.
        var rankedSessions = rankedDocuments.TopMatches
            .Concat(rankedDocuments.DistinctSources)
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
            || match.Summary.PromptCount >= MinimumPromptCount)
        && (MemoryActivityRange.ToUtc(ActiveFrom) is not { } activeFrom
            || match.Summary.LastActivityTimestamp >= activeFrom)
        && (MemoryActivityRange.ToUtc(ActiveTo) is not { } activeTo
            || (match.Summary.FirstPromptTimestamp
                ?? match.Summary.LastActivityTimestamp) <= activeTo);

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
