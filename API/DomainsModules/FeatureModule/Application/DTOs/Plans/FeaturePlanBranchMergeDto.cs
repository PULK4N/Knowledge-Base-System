using FeatureModule.Domain.Models;

namespace FeatureModule.Application.DTOs;

/// <summary>
/// The branch a base branch of a completed plan was merged into: the feature
/// branch when IntoPlanId is null, otherwise a branch of another plan.
/// </summary>
public sealed record FeaturePlanBranchMergeDto(
    string BranchName,
    Guid? IntoPlanId,
    string IntoBranchName
)
{
    public FeaturePlanBranchSource ToSource() =>
        new FeaturePlanBranchSourceDto(IntoPlanId, IntoBranchName).ToModel();
}
