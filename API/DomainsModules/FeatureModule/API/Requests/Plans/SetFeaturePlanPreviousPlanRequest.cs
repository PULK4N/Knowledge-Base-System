namespace FeatureModule.API.Requests;

public sealed record SetFeaturePlanPreviousPlanRequest
{
    public Guid? PreviousPlanId { get; init; }
}
