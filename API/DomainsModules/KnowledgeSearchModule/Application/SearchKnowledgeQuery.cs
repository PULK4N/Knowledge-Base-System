using ActionModule.Shared;
using ActionModule.Shared.Models;
using EmbeddingModule;

namespace KnowledgeSearchModule.Application;

public sealed class SearchKnowledgeQuery(
    IKnowledgeSearch knowledgeSearch
) : Query<List<KnowledgeSearchMatchDto>>
{
    public const int DefaultResultCount =
        HybridKnowledgeSearchOptions.DefaultResultCount;
    public const int MinimumResultCount = 1;
    public const int MaximumResultCount = 50;
    public const int MaximumSearchTextLength =
        KnowledgeSearchQueryLimits.MaximumLength;

    public required string SearchText { get; set; }
    public int ResultCount { get; set; } = DefaultResultCount;

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            !string.IsNullOrWhiteSpace(SearchText)
            && SearchText.Length <= MaximumSearchTextLength
            && ResultCount is >= MinimumResultCount
                and <= MaximumResultCount
        );

    protected override async Task<
        List<KnowledgeSearchMatchDto>
    > ExecuteInternal(Executor executor) =>
        (await knowledgeSearch.Search(
            SearchText,
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
                )
            }
        ))
            .Select(KnowledgeSearchMatchDto.FromResult)
            .ToList();
}
