namespace ProjectModule.Persistence.Interfaces;

public interface IPolicyProjectSummaryRepository
{
    Task<List<PolicyProjectSummary>> List();

    Task<PolicyProjectSummary?> GetByName(
        string name,
        CancellationToken cancellationToken = default
    );

    Task<PolicyProjectSummarySearchResult> Search(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default
    );
}
