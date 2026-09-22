using ActionModule.Shared;
using ActionModule.Shared.Models;
using EmbeddingModule;

namespace KnowledgeSearchModule.Application;

public sealed class SearchKnowledgeQuery(
    IKnowledgeSearch knowledgeSearch
) : Query<KnowledgeSearchResultsDto>
{
    public const int DefaultResultCount =
        HybridKnowledgeSearchOptions.DefaultResultCount;
    public const int MinimumResultCount = 1;
    public const int MaximumResultCount = 50;
    public const int MaximumSearchTextLength =
        KnowledgeSearchQueryLimits.MaximumLength;

    public required string SearchText { get; set; }

    /// <summary>
    /// Words the full-text leg requires. The semantic leg reads SearchText.
    /// </summary>
    public List<string> Keywords { get; set; } = [];

    public int ResultCount { get; set; } = DefaultResultCount;

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            !string.IsNullOrWhiteSpace(SearchText)
            && SearchText.Length <= MaximumSearchTextLength
            && SearchKeywordLimits.AreValid(Keywords)
            && ResultCount is >= MinimumResultCount
                and <= MaximumResultCount
        );

    protected override async Task<
        KnowledgeSearchResultsDto
    > ExecuteInternal(Executor executor) =>
        KnowledgeSearchResultsDto.FromResults(
            await knowledgeSearch.SearchWithSources(
                SearchText,
                Keywords,
                new HybridKnowledgeSearchOptions
                {
                    ResultCount = ResultCount,
                    CandidateCount = Math.Max(
                        HybridKnowledgeSearchOptions.DefaultCandidateCount,
                        Math.Min(
                            HybridKnowledgeSearchOptions.MaximumCandidateCount,
                            ResultCount
                                * HybridKnowledgeSearchOptions
                                    .DeduplicationOverfetchMultiplier
                        )
                    ),
                    SourceResultCount = ResultCount
                }
            )
        );
}
