using ProjectModule.Shared;

namespace FeatureModule.API.Requests;

public sealed record RemoveFeatureReviewerRequest
{
    public required Guid ReviewerId { get; init; }
}
