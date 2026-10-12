namespace FeatureModule.API.Requests;

public sealed record UpdateFeaturePlanBranchRequest
{
    public required string BranchName { get; init; }

    public string? NewBranchName { get; init; }

    public string Description { get; init; } = string.Empty;
}
