using ActionModule.Shared;
using ActionModule.Shared.Models;
using FeatureModule.Application.DTOs;
using FeatureModule.Contracts;
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
    IFeatureSearchRepository featureSearchRepository
) : PagedQuery<FeatureSummaryDto>
{
    public Guid? ProjectId { get; set; }
    public FeatureSearchSortField SortBy { get; set; } =
        FeatureSearchSortField.Name;
    public SortDirection SortDirection { get; set; } =
        SortDirection.Ascending;

    public override async Task<bool> CanExecute(Executor executor) =>
        await base.CanExecute(executor)
        && Enum.IsDefined(SortBy)
        && Enum.IsDefined(SortDirection);

    protected override async Task<PagedResult<FeatureSummaryDto>> ExecuteInternal(
        Executor executor
    )
    {
        var result = await featureSearchRepository.Search(
            CreateEntityQuery(
                new FeatureSearchFilters(ProjectId),
                SortBy,
                SortDirection
            )
        );

        return result.Map(FeatureSummaryDto.FromReadModel);
    }
}
