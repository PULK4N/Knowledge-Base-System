namespace ProjectModule.API.Requests;

public sealed record AddRepositoryToProjectRequest
{
    public required Guid ProjectId { get; init; }

    public required string RepositoryPath { get; init; }
}
