namespace ProjectModule.API.Requests;

public sealed record UpdateProjectRequest
{
    public required Guid ProjectId { get; init; }

    public required string ProjectName { get; init; }

    public required string ProjectDescription { get; init; }
}
