namespace ProjectModule.Persistence.Interfaces;

public sealed record PolicyProjectSummary(
    Guid ProjectId,
    string ProjectName,
    List<string> RepositoryPaths
);
