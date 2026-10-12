namespace FeatureModule.API.Requests;

public sealed record SetFeatureBranchRequest
{
    public required string BranchName { get; init; }

    public string Description { get; init; } = string.Empty;
}
