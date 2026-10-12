namespace FeatureModule.API.Requests;

public sealed record RemoveFeaturePlanBranchRequest
{
    public required string BranchName { get; init; }
}
