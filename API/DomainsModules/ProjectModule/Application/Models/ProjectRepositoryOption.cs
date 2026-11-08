namespace ProjectModule.Application.Models;

public sealed record ProjectRepositoryOption(
    Guid ProjectId,
    string ProjectName,
    List<string> RepositoryPaths
);
