using ActionModule.Shared;
using ActionModule.Shared.Models;
using FeatureModule.Application.DTOs;
using FeatureModule.Persistence.Interfaces;

namespace FeatureModule.Application.Queries;

public sealed class SearchFeaturesQuery(
    IFeatureSummaryRepository featureSummaryRepository
) : Query<PagedResult<FeatureSummaryDto>>
{
    public int Page { get; set; } = Pagination.DefaultPage;
    public int PageSize { get; set; } = Pagination.DefaultPageSize;
    public string? Search { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(Pagination.IsValid(Page, PageSize));

    protected override async Task<PagedResult<FeatureSummaryDto>> ExecuteInternal(
        Executor executor
    )
    {
        var result = await featureSummaryRepository.Search(
            Page,
            PageSize,
            Search
        );

        return new PagedResult<FeatureSummaryDto>(
            result.Items
                .Select(FeatureSummaryDto.FromReadModel)
                .ToList(),
            Page,
            PageSize,
            result.TotalCount
        );
    }
}
