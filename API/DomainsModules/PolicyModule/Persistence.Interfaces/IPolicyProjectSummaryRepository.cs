namespace PolicyModule.Persistence.Interfaces;

public interface IPolicyProjectSummaryRepository
{
    Task<List<PolicyProjectSummary>> List();

    Task<PolicyProjectSummarySearchResult> Search(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default
    );
}

public sealed record PolicyProjectSummary(
    Guid ProjectId,
    string ProjectName,
    List<string> RepositoryPaths
);

public sealed record PolicyProjectSummarySearchResult(
    List<PolicyProjectSummary> Items,
    int TotalCount
);
