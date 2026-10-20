using FeatureModule.Application.DTOs;

namespace FeatureModule.MCP;

public static class FeaturePlanBranchTreeFormatter
{
    public static string? Format(FeaturePlanBranchDto? branch) =>
        branch is null ? null : TextTree.Render(ToNode(branch, []));

    /// <summary>
    /// Renders every base branch of a plan as its own tree, each base line
    /// followed by the branches it came from.
    /// </summary>
    public static string? FormatBaseBranches(FeaturePlanDto plan, FeatureDto feature)
    {
        if (plan.Branches.Count == 0)
            return null;

        var defaultSource = DefaultSourceName(plan, feature);

        return string.Join(
            '\n',
            plan.Branches.Select(
                branch => TextTree.Render(
                    ToNode(
                        branch,
                        [],
                        $"  (from: {string.Join(", ", SourceNames(branch, feature, defaultSource))})"
                    )
                )
            )
        );
    }

    /// <summary>
    /// Builds the node for a branch; <paramref name="extraChildren"/> follow its sub-branches.
    /// </summary>
    public static TextTreeNode ToNode(
        FeaturePlanBranchDto branch,
        IReadOnlyList<TextTreeNode> extraChildren,
        string label = ""
    ) =>
        new(
            BranchLine(branch.Name, branch.Description) + label,
            [
                .. branch.SubBranches.Select(subBranch => ToNode(subBranch, [])),
                .. extraChildren
            ]
        );

    private static IEnumerable<string> SourceNames(
        FeaturePlanBranchDto branch,
        FeatureDto feature,
        string defaultSource
    ) =>
        branch.Sources.Count == 0
            ? [defaultSource]
            : branch.Sources.Select(
                source => source.PlanId is null
                    ? feature.Branch?.Name ?? "feature branch"
                    : source.BranchName
            );

    /// <summary>
    /// Without sources a base branch comes from the previous plan's first base
    /// branch, skipping plans without one, or from the feature branch.
    /// </summary>
    private static string DefaultSourceName(FeaturePlanDto plan, FeatureDto feature)
    {
        var plans = feature.Plans.ToDictionary(item => item.Id);
        var visited = new HashSet<Guid> { plan.Id };
        var previousPlanId = plan.PreviousPlanId;

        while (
            previousPlanId is { } id
            && visited.Add(id)
            && plans.TryGetValue(id, out var previous)
        )
        {
            if (previous.Branches.Count > 0)
                return previous.Branches[0].Name;

            previousPlanId = previous.PreviousPlanId;
        }

        return feature.Branch?.Name ?? "feature branch";
    }

    public static string BranchLine(string name, string description)
    {
        var singleLine = TextTree.SingleLine(description);

        return singleLine.Length == 0 ? name : $"{name} - {singleLine}";
    }
}
