namespace FeatureModule.API.Requests;

public sealed record AddFeaturePlanBranchRequest
{
    public required string BranchName { get; init; }

    public string Description { get; init; } = string.Empty;

    public string? ParentBranchName { get; init; }
}
