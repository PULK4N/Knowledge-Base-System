using FeatureModule.Domain.Models;

namespace FeatureModule.Application.DTOs;

/// <summary>
/// A branch a base branch was created from: the feature branch when PlanId is
/// null, otherwise the named branch of another plan.
/// </summary>
public sealed record FeaturePlanBranchSourceDto(Guid? PlanId, string BranchName)
{
    public static FeaturePlanBranchSourceDto FromModel(
        FeaturePlanBranchSource source
    ) =>
        new(source.PlanId?.Value, source.BranchName);

    public FeaturePlanBranchSource ToModel() =>
        PlanId is { } planId
            ? new FeaturePlanBranchSource(
                FeaturePlanId.FromDatabaseGuid(planId),
                BranchName.Trim()
            )
            : FeaturePlanBranchSource.FeatureBranch;
}
