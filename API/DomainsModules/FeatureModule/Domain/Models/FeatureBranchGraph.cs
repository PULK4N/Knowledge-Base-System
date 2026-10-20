namespace FeatureModule.Domain.Models;

/// <summary>
/// Identifies a branch in a feature: the feature branch when PlanId is null,
/// otherwise a branch of that plan.
/// </summary>
public readonly record struct FeatureBranchKey(FeaturePlanId? PlanId, string BranchName)
{
    public static FeatureBranchKey FeatureBranch { get; } = new(null, string.Empty);
}

/// <summary>
/// The branches of a feature and where each came from. A branch can come from
/// several branches, so this is a graph, not a tree:
/// <code>
/// main ──► x1-api ──┐
///    └───► x1-web ──┴──► x2
/// </code>
/// A base branch comes from its sources, or by default from the previous
/// plan's first base branch (skipping plans without one) or the feature
/// branch. A sub-branch comes from its parent.
/// </summary>
public static class FeatureBranchGraph
{
    /// <summary>
    /// Every branch with the branches it came from, in plan creation order,
    /// base branches first and each followed by its sub-branches.
    /// </summary>
    public static List<(FeatureBranchKey Branch, List<FeatureBranchKey> From)> Build(
        IReadOnlyCollection<FeaturePlan> plans,
        Func<FeaturePlan, FeaturePlanBranch, IReadOnlyList<FeaturePlanBranchSource>>? sourcesOf = null,
        Func<FeaturePlan, FeaturePlanId?>? previousPlanOf = null
    )
    {
        sourcesOf ??= (_, branch) => branch.Sources;
        previousPlanOf ??= plan => plan.PreviousPlanId;
        var plansById = plans.ToDictionary(plan => plan.Id);
        var edges = new List<(FeatureBranchKey, List<FeatureBranchKey>)>();

        foreach (var plan in plans.OrderBy(plan => plan.CreatedAt))
        {
            foreach (var baseBranch in plan.Branches)
            {
                var sources = sourcesOf(plan, baseBranch);
                edges.Add((
                    new FeatureBranchKey(plan.Id, baseBranch.Name),
                    sources.Count == 0
                        ? [DefaultSource(plan, plansById, previousPlanOf)]
                        : ResolveSources(sources, plansById)
                ));
                AddSubBranches(plan.Id, baseBranch, edges);
            }
        }

        return edges;
    }

    /// <summary>
    /// True when following where branches came from leads back to a branch.
    /// </summary>
    public static bool HasCycle(
        IReadOnlyCollection<(FeatureBranchKey Branch, List<FeatureBranchKey> From)> edges
    )
    {
        var from = edges.ToDictionary(edge => edge.Branch, edge => edge.From);
        var done = new HashSet<FeatureBranchKey>();
        var visiting = new HashSet<FeatureBranchKey>();

        bool Visit(FeatureBranchKey key)
        {
            if (done.Contains(key))
                return false;
            if (!visiting.Add(key))
                return true;

            var cycle = from.TryGetValue(key, out var sources) && sources.Any(Visit);
            visiting.Remove(key);
            done.Add(key);

            return cycle;
        }

        return from.Keys.Any(Visit);
    }

    private static FeatureBranchKey DefaultSource(
        FeaturePlan plan,
        Dictionary<FeaturePlanId, FeaturePlan> plansById,
        Func<FeaturePlan, FeaturePlanId?> previousPlanOf
    )
    {
        var visited = new HashSet<FeaturePlanId> { plan.Id };
        var previousPlanId = previousPlanOf(plan);

        while (
            previousPlanId is { } id
            && visited.Add(id)
            && plansById.TryGetValue(id, out var previous)
        )
        {
            if (previous.Branches.Count > 0)
                return new FeatureBranchKey(previous.Id, previous.Branches[0].Name);

            previousPlanId = previousPlanOf(previous);
        }

        return FeatureBranchKey.FeatureBranch;
    }

    private static List<FeatureBranchKey> ResolveSources(
        IReadOnlyList<FeaturePlanBranchSource> sources,
        Dictionary<FeaturePlanId, FeaturePlan> plansById
    )
    {
        var keys = sources
            .Select(
                source => source.PlanId is { } planId
                    ? plansById.TryGetValue(planId, out var plan)
                        && plan.FindBranch(source.BranchName) is not null
                        ? new FeatureBranchKey(planId, source.BranchName)
                        : (FeatureBranchKey?)null
                    : FeatureBranchKey.FeatureBranch
            )
            .OfType<FeatureBranchKey>()
            .Distinct()
            .ToList();

        return keys.Count == 0 ? [FeatureBranchKey.FeatureBranch] : keys;
    }

    private static void AddSubBranches(
        FeaturePlanId planId,
        FeaturePlanBranch parent,
        List<(FeatureBranchKey, List<FeatureBranchKey>)> edges
    )
    {
        foreach (var subBranch in parent.SubBranches)
        {
            edges.Add((
                new FeatureBranchKey(planId, subBranch.Name),
                [new FeatureBranchKey(planId, parent.Name)]
            ));
            AddSubBranches(planId, subBranch, edges);
        }
    }
}
