namespace ProjectModule.Persistence.Interfaces;

public sealed record PolicyProjectSummarySearchResult(
    List<PolicyProjectSummary> Items,
    int TotalCount
);
