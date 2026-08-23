using ActionModule.Shared.Models;
using FeatureModule.Contracts;

namespace FeatureModule.Persistence.Interfaces;

public interface IFeatureSearchRepository
{
    Task<PagedResult<FeatureSummary>> Search(
        EntityQuery<FeatureSearchFilters, FeatureSearchSortField> request,
        CancellationToken cancellationToken = default
    );
}
