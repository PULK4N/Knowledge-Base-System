namespace FeatureModule.Domain.Models;

/// <summary>
/// Works out which base branches of other plans must change their sources
/// when a plan is completed, because its branches were merged away. With
/// x1 ← x2 ← x3, merging x2 into x1 makes x3 come from x1.
/// </summary>
public static class FeaturePlanMerge
{
    /// <param name="mergedInto">
    /// The branch each base branch of the completed plan was merged into, by
    /// base branch name. A base branch that is not listed was merged into its
    /// own sources. A sub-branch counts as merged into what its base branch
    /// was merged into.
    /// </param>
    /// <returns>The new sources of every base branch that came from the completed plan.</returns>
    public static List<(FeaturePlanId PlanId, string BranchName, List<FeaturePlanBranchSource> Sources)> RewriteFollowers(
        IReadOnlyCollection<FeaturePlan> plans,
        FeaturePlanId completedPlanId,
        IReadOnlyDictionary<string, FeaturePlanBranchSource> mergedInto
    )
    {
        var completed = plans.Single(plan => plan.Id == completedPlanId);
        Validate(plans, completed, mergedInto);

        var from = FeatureBranchGraph.Build(plans)
            .ToDictionary(edge => edge.Branch, edge => edge.From);
        var targets = new Dictionary<string, List<FeaturePlanBranchSource>>();
        foreach (var baseBranch in completed.Branches)
        {
            List<FeaturePlanBranchSource> target = mergedInto.TryGetValue(baseBranch.Name, out var into)
                ? [into]
                : from[new FeatureBranchKey(completed.Id, baseBranch.Name)].Select(ToSource).ToList();

            foreach (var branch in baseBranch.SelfAndDescendants())
                targets[branch.Name] = target;
        }

        var rewrites = new List<(FeaturePlanId, string, List<FeaturePlanBranchSource>)>();
        foreach (var plan in plans.Where(plan => plan.Id != completedPlanId))
        {
            foreach (var baseBranch in plan.Branches)
            {
                var sources = from[new FeatureBranchKey(plan.Id, baseBranch.Name)];
                if (!sources.Any(source => source.PlanId == completedPlanId))
                    continue;

                rewrites.Add((
                    plan.Id,
                    baseBranch.Name,
                    sources
                        .SelectMany(
                            source => source.PlanId == completedPlanId
                                ? targets[source.BranchName]
                                : [ToSource(source)]
                        )
                        .Distinct()
                        .ToList()
                ));
            }
        }

        return rewrites;
    }

    private static void Validate(
        IReadOnlyCollection<FeaturePlan> plans,
        FeaturePlan completed,
        IReadOnlyDictionary<string, FeaturePlanBranchSource> mergedInto
    )
    {
        foreach (var (branchName, into) in mergedInto)
        {
            if (completed.FindBaseBranch(branchName) is null)
                throw new InvalidOperationException(
                    $"The plan does not have a base branch named '{branchName}'."
                );

            if (into.PlanId == completed.Id)
                throw new InvalidOperationException(
                    "A branch cannot be merged into a branch of its own plan."
                );

            if (
                into.PlanId is { } planId
                && plans.SingleOrDefault(plan => plan.Id == planId)
                    ?.FindBranch(into.BranchName) is null
            )
                throw new InvalidOperationException(
                    $"The branch '{into.BranchName}' that '{branchName}' was merged into does not exist."
                );
        }
    }

    private static FeaturePlanBranchSource ToSource(FeatureBranchKey key) =>
        key.PlanId is { } planId
            ? new FeaturePlanBranchSource(planId, key.BranchName)
            : FeaturePlanBranchSource.FeatureBranch;
}
