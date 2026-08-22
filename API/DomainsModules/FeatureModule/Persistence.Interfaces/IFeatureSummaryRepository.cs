namespace FeatureModule.Persistence.Interfaces;

public interface IFeatureSummaryRepository
{
    Task<List<FeatureSummary>> List(
        CancellationToken cancellationToken = default
    );

    Task<FeatureSummary?> GetByName(
        string name,
        CancellationToken cancellationToken = default
    );

    Task<FeatureSummarySearchResult> Search(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default
    );
}

public sealed record FeatureSummary(
    Guid FeatureId,
    Guid ProjectId,
    string Name,
    string Summary,
    string Status,
    Guid? CurrentPlanId,
    int PlanCount,
    int RecordCount
);

public sealed record FeatureSummarySearchResult(
    List<FeatureSummary> Items,
    int TotalCount
);
