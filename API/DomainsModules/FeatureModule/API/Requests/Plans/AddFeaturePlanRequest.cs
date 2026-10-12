namespace FeatureModule.API.Requests;

public sealed record AddFeaturePlanRequest : FeaturePlanContentRequest
{
    public required string BranchName { get; init; }

    public string BranchDescription { get; init; } = string.Empty;
}
