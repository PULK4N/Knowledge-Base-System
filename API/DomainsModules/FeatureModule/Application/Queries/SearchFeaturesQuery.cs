using ActionModule.Shared;
using ActionModule.Shared.Models;
using FeatureModule.Application.DTOs;
using FeatureModule.Persistence.Interfaces;

namespace FeatureModule.Application.Queries;

public sealed class ListFeaturesQuery(
    IFeatureSummaryRepository featureSummaryRepository
) : Query<List<FeatureSummaryDto>>
{
    protected override async Task<List<FeatureSummaryDto>> ExecuteInternal(
        Executor executor
    ) =>
        (await featureSummaryRepository.List())
            .Select(FeatureSummaryDto.FromReadModel)
            .ToList();
}

public sealed class GetFeatureByNameQuery(
    IFeatureSummaryRepository featureSummaryRepository
) : Query<FeatureSummaryDto?>
{
    public required string Name { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(Name));

    protected override async Task<FeatureSummaryDto?> ExecuteInternal(
        Executor executor
    )
    {
        var feature = await featureSummaryRepository.GetByName(Name);

        return feature is null
            ? null
            : FeatureSummaryDto.FromReadModel(feature);
    }
}

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
