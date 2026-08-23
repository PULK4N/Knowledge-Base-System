namespace FeatureModule.Contracts;

public sealed record FeatureSearchFilters(Guid? ProjectId);

public enum FeatureSearchSortField
{
    Name,
    PlanCount,
    RecordCount
}
