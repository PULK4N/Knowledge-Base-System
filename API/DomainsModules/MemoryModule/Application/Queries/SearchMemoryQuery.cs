using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;
using MemoryModule.Application.DTOs;
using MemoryModule.Domain;
using MemoryModule.Persistence.Interfaces;

namespace MemoryModule.Application.Queries;

public sealed class SearchMemoryQuery(
    StateCalculator stateCalculator,
    IEventStore eventStore,
    IMemorySearch memorySearch
) : MemoryQuery<MemorySearchQueryResult>(stateCalculator, eventStore)
{
    public const int MaximumResultCount = 2;
    public const int DefaultMaximumTokens = 2000;
    public const int MinimumMaximumTokens = 128;
    public const int MaximumMaximumTokens = 4000;

    private const int RankedChunkCount = 20;
    private const int CandidateChunkCount = 50;
    private const double DuplicateSummarySimilarity = 0.9;

    public required string SearchText { get; set; }
    public int MaxTokens { get; set; } = DefaultMaximumTokens;

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            !string.IsNullOrWhiteSpace(SearchText)
            && MaxTokens is >= MinimumMaximumTokens
                and <= MaximumMaximumTokens
        );

    protected override async Task<MemorySearchQueryResult> ExecuteInternal(
        Executor executor
    )
    {
        var rankedChunks = await memorySearch.Search(
            SearchText,
            new HybridMemorySearchOptions
            {
                ResultCount = RankedChunkCount,
                CandidateCount = CandidateChunkCount
            }
        );
        var sessionCandidates = rankedChunks
            .GroupBy(result => result.Memory.MemoryAggregateId)
            .Select(group => group.First())
            .ToList();
        var states = await GetStates(
            sessionCandidates
                .Select(result => result.Memory.MemoryAggregateId)
                .ToList()
        );
        var selected = SelectDistinctSessions(
            sessionCandidates,
            states
        );
        var message = selected.Count == 0
            ? "No relevant chat memories were found."
            : $"Found {selected.Count} relevant chat memory session(s). Use the prompt window query only when surrounding messages are needed.";
        var budget = new ApproximateTokenBudget(MaxTokens);
        budget.Take(message);
        var results = selected
            .Select(
                selection => ToDto(
                    selection.Result,
                    selection.State,
                    budget
                )
            )
            .ToList();

        return new MemorySearchQueryResult(
            message,
            results,
            budget.ApproximateTokenCount,
            budget.IsTruncated
        );
    }

    private static List<SessionSelection> SelectDistinctSessions(
        List<MemorySearchResult> sessionCandidates,
        Dictionary<AggregateId, MemoryStateData> states
    )
    {
        var selected = new List<SessionSelection>();
        var summaryTerms = new List<HashSet<string>>();

        foreach (var candidate in sessionCandidates)
        {
            if (!states.TryGetValue(
                    candidate.Memory.MemoryAggregateId,
                    out var state
                ))
            {
                continue;
            }

            var terms = GetTerms(state.ChatSummary.Summary);
            if (terms.Count > 0
                && summaryTerms.Any(
                    existing => Similarity(existing, terms)
                        >= DuplicateSummarySimilarity
                ))
            {
                continue;
            }

            if (terms.Count > 0)
                summaryTerms.Add(terms);
            selected.Add(new SessionSelection(candidate, state));
            if (selected.Count == MaximumResultCount)
                break;
        }

        return selected;
    }

    private static MemorySearchMatchDto ToDto(
        MemorySearchResult result,
        MemoryStateData state,
        ApproximateTokenBudget budget
    )
    {
        var matchedSummary = string.Equals(
            result.Memory.HookEventName,
            MemorySearchDocumentSources.ChatSummary,
            StringComparison.Ordinal
        );

        return new MemorySearchMatchDto(
            state.Id.Value,
            state.ThreadId.Value,
            matchedSummary ? null : result.Memory.PromptId.Value,
            result.Memory.PromptStartTimestamp,
            matchedSummary,
            budget.Take(state.ChatSummary.Summary) ?? string.Empty,
            matchedSummary
                ? null
                : budget.Take(result.Memory.Text)
        );
    }

    private static HashSet<string> GetTerms(string text) =>
        text.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries
            )
            .Select(
                term => new string(
                    term
                        .Where(char.IsLetterOrDigit)
                        .Select(char.ToUpperInvariant)
                        .ToArray()
                )
            )
            .Where(term => term.Length > 0)
            .ToHashSet(StringComparer.Ordinal);

    private static double Similarity(
        HashSet<string> first,
        HashSet<string> second
    )
    {
        var intersectionCount = first.Intersect(second).Count();
        var unionCount = first.Count + second.Count - intersectionCount;

        return unionCount == 0
            ? 1
            : (double)intersectionCount / unionCount;
    }

    private sealed record SessionSelection(
        MemorySearchResult Result,
        MemoryStateData State
    );
}
