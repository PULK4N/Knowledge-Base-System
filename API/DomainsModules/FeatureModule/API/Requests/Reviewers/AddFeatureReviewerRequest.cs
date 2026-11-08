using ProjectModule.Shared;

namespace FeatureModule.API.Requests;

public sealed record AddFeatureReviewerRequest
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required ReviewLevel ReviewLevel { get; init; }
}
