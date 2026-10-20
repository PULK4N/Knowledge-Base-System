using FeatureModule.Application.DTOs;

namespace FeatureModule.API.Requests;

public sealed record AddFeaturePlanBranchRequest
{
    public required string BranchName { get; init; }

    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Null adds another base branch to the plan.
    /// </summary>
    public string? ParentBranchName { get; init; }

    /// <summary>
    /// Only for a new base branch: the branches it was created from.
    /// </summary>
    public List<FeaturePlanBranchSourceDto> Sources { get; init; } = [];
}
