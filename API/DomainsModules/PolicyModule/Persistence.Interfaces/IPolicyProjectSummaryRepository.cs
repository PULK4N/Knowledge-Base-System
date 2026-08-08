namespace PolicyModule.Persistence.Interfaces;

public interface IPolicyProjectSummaryRepository
{
    Task<List<PolicyProjectSummary>> List();
}

public sealed record PolicyProjectSummary(
    Guid ProjectId,
    string ProjectName,
    List<string> RepositoryPaths
);
