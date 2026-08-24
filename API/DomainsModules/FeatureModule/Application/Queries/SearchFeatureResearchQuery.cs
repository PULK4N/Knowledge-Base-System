using ActionModule.Shared;
using ActionModule.Shared.Models;
using FeatureModule.Application.DTOs;
using FeatureModule.Persistence.Interfaces;

namespace FeatureModule.Application.Queries;

public sealed class SearchFeatureResearchQuery(
    IFeatureResearchSearch featureResearchSearch
) : Query<List<FeatureResearchSearchMatchDto>>
{
    public const int DefaultResultCount =
        HybridFeatureResearchSearchOptions.DefaultResultCount;
    public const int MinimumResultCount = 1;
    public const int MaximumResultCount = 20;

    private const int CandidateCount = 50;

    public required string SearchText { get; set; }
    public int ResultCount { get; set; } = DefaultResultCount;

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            !string.IsNullOrWhiteSpace(SearchText)
            && ResultCount is >= MinimumResultCount
                and <= MaximumResultCount
        );

    protected override async Task<
        List<FeatureResearchSearchMatchDto>
    > ExecuteInternal(Executor executor) =>
        (await featureResearchSearch.Search(
            SearchText,
            new HybridFeatureResearchSearchOptions
            {
                ResultCount = ResultCount,
                CandidateCount = CandidateCount
            }
        ))
            .Select(FeatureResearchSearchMatchDto.FromSearchResult)
            .ToList();
}
