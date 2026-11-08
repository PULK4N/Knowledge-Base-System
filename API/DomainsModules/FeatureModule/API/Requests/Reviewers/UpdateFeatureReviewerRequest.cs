using ProjectModule.Shared;

namespace FeatureModule.API.Requests;

public sealed record UpdateFeatureReviewerRequest
{
    public required Guid ReviewerId { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required ReviewLevel ReviewLevel { get; init; }
}
