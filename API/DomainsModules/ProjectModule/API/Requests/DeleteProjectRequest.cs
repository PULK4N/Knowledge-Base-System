namespace ProjectModule.API.Requests;

public sealed record DeleteProjectRequest
{
    public required Guid ProjectId { get; init; }
}
