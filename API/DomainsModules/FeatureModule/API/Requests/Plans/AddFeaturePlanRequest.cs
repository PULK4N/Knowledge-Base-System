using FeatureModule.Application.DTOs;

namespace FeatureModule.API.Requests;

public sealed record AddFeaturePlanRequest : FeaturePlanContentRequest
{
    public required string BranchName { get; init; }

    public string BranchDescription { get; init; } = string.Empty;

    public Guid? PreviousPlanId { get; init; }

    /// <summary>
    /// The branches the base branch was created from; empty keeps the default.
    /// </summary>
    public List<FeaturePlanBranchSourceDto> BranchSources { get; init; } = [];
}
