using System.Text.RegularExpressions;
using FeatureModule.Application.DTOs;

namespace FeatureModule.MCP;

/// <summary>
/// Builds one tree for a feature: the feature branch at the root, each plan's base
/// branch under the base branch of the plan done before it (or under the feature
/// branch), and each plan's own sub-branches beneath its base branch.
/// </summary>
public static partial class FeatureBranchTreeFormatter
{
    public static string? Format(FeatureDto feature)
    {
        if (feature.Branch is null && feature.Plans.Count == 0)
            return null;

        var planIds = feature.Plans.Select(plan => plan.Id).ToHashSet();
        var followers = feature.Plans
            .OrderBy(plan => plan.CreatedAt)
            .ToLookup(plan =>
                plan.PreviousPlanId is { } previousPlanId && planIds.Contains(previousPlanId)
                    ? previousPlanId
                    : (Guid?)null
            );
        var root = new TextTreeNode(
            feature.Branch is null
                ? "(feature branch not set)"
                : FeaturePlanBranchTreeFormatter.BranchLine(
                    feature.Branch.Name,
                    feature.Branch.Description
                ),
            [.. followers[null].Select(plan => PlanNode(plan, followers))]
        );

        return TextTree.Render(root);
    }

    private static TextTreeNode PlanNode(
        FeaturePlanDto plan,
        ILookup<Guid?, FeaturePlanDto> followers
    )
    {
        var label = $"  [{TextTree.SingleLine(plan.Title)} · {StatusLabel(plan.Status.ToString())}]";
        List<TextTreeNode> nextPlans =
            [.. followers[plan.Id].Select(next => PlanNode(next, followers))];

        return plan.Branch is null
            ? new TextTreeNode("(no base branch)" + label, nextPlans)
            : FeaturePlanBranchTreeFormatter.ToNode(plan.Branch, nextPlans, label);
    }

    private static string StatusLabel(string status)
    {
        var words = WordStart().Replace(status, " $1").Trim().ToLowerInvariant();

        return char.ToUpperInvariant(words[0]) + words[1..];
    }

    [GeneratedRegex("([A-Z])")]
    private static partial Regex WordStart();
}
