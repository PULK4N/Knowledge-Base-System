namespace ProjectModule.API.Requests;

public sealed record AddProjectPolicyRequest
{
    public required Guid ProjectId { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }
}
