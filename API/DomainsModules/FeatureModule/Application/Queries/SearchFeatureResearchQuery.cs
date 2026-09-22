using ActionModule.Shared;
using ActionModule.Shared.Models;
using FeatureModule.Application.DTOs;
using EmbeddingModule;
using FeatureModule.Persistence.Interfaces;

namespace FeatureModule.Application.Queries;

public sealed class SearchFeatureResearchQuery(
    IFeatureResearchSearch featureResearchSearch
) : Query<FeatureResearchSearchResultsDto>
{
    public const int DefaultResultCount =
        HybridFeatureResearchSearchOptions.DefaultResultCount;
    public const int MinimumResultCount = 1;
    public const int MaximumResultCount = 20;

    private const int CandidateCount = 50;

    public required string SearchText { get; set; }

    /// <summary>
    /// Words the full-text leg requires. The semantic leg reads SearchText.
    /// </summary>
    public List<string> Keywords { get; set; } = [];

    public int ResultCount { get; set; } = DefaultResultCount;

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            !string.IsNullOrWhiteSpace(SearchText)
            && SearchKeywordLimits.AreValid(Keywords)
            && ResultCount is >= MinimumResultCount
                and <= MaximumResultCount
        );

    protected override async Task<
        FeatureResearchSearchResultsDto
    > ExecuteInternal(Executor executor) =>
        FeatureResearchSearchResultsDto.FromSearchResults(
            await featureResearchSearch.SearchWithSources(
                SearchText,
                Keywords,
                new HybridFeatureResearchSearchOptions
                {
                    ResultCount = ResultCount,
                    CandidateCount = CandidateCount,
                    SourceResultCount = ResultCount
                }
            )
        );
}
