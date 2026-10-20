namespace FeatureModule.API.Requests;

public sealed record SetFeatureCompositeBranchRequest
{
    public required string BranchName { get; init; }
}
