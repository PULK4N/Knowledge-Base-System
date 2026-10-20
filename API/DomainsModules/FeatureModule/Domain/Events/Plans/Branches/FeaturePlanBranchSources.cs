using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

/// <summary>
/// Keeps base-branch sources pointing at branches that still exist when a
/// branch is renamed or removed, or a plan is removed.
/// </summary>
internal static class FeaturePlanBranchSources
{
    internal static void Remove(
        FeatureStateData state,
        Func<FeaturePlanBranchSource, bool> isRemoved
    )
    {
        foreach (var baseBranch in state.Plans.SelectMany(plan => plan.Branches))
            baseBranch.Sources.RemoveAll(source => isRemoved(source));
    }

    internal static void Rename(
        FeatureStateData state,
        FeaturePlanId planId,
        string branchName,
        string newBranchName
    )
    {
        var renamed = new FeaturePlanBranchSource(planId, branchName);

        foreach (var baseBranch in state.Plans.SelectMany(plan => plan.Branches))
        {
            var index = baseBranch.Sources.IndexOf(renamed);
            if (index >= 0)
                baseBranch.Sources[index] = renamed with { BranchName = newBranchName };
        }
    }
}
