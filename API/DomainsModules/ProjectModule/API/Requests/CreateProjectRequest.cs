namespace ProjectModule.API.Requests;

public sealed record CreateProjectRequest
{
    public required string ProjectName { get; init; }

    public required string ProjectDescription { get; init; }

    public List<string> RepositoryPaths { get; init; } = [];
}
