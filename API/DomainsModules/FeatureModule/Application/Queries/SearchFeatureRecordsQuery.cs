using ActionModule.Shared;
using ActionModule.Shared.Models;
using FeatureModule.Application.DTOs;
using EmbeddingModule;
using FeatureModule.Persistence.Interfaces;

namespace FeatureModule.Application.Queries;

public sealed class SearchFeatureRecordsQuery(
    IFeatureRecordSearch featureRecordSearch
) : Query<FeatureRecordSearchResultsDto>
{
    public const int DefaultResultCount =
        HybridFeatureRecordSearchOptions.DefaultResultCount;
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
        FeatureRecordSearchResultsDto
    > ExecuteInternal(Executor executor) =>
        FeatureRecordSearchResultsDto.FromSearchResults(
            await featureRecordSearch.SearchWithSources(
                SearchText,
                Keywords,
                new HybridFeatureRecordSearchOptions
                {
                    ResultCount = ResultCount,
                    CandidateCount = CandidateCount,
                    SourceResultCount = ResultCount
                }
            )
        );
}
