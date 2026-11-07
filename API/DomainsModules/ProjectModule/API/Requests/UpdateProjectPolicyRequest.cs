namespace ProjectModule.API.Requests;

public sealed record UpdateProjectPolicyRequest
{
    public required Guid ProjectId { get; init; }

    public required Guid PolicyId { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }
}
