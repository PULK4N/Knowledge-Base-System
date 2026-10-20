using FeatureModule.Application.DTOs;
using FeatureModule.Domain.Models;

namespace FeatureModule.API.Requests;

public sealed record ChangeFeaturePlanStatusRequest
{
    public required FeaturePlanStatus Status { get; init; }

    /// <summary>
    /// Only when completing: where each base branch was merged; unlisted base
    /// branches were merged into their own sources.
    /// </summary>
    public List<FeaturePlanBranchMergeDto> MergedInto { get; init; } = [];
}
