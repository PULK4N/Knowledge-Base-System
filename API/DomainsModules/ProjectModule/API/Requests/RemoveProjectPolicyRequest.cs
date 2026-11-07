namespace ProjectModule.API.Requests;

public sealed record RemoveProjectPolicyRequest
{
    public required Guid ProjectId { get; init; }

    public required Guid PolicyId { get; init; }
}
