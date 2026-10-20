using System.Text.RegularExpressions;
using FeatureModule.Application.DTOs;
using FeatureModule.Domain.Models;

namespace FeatureModule.MCP;

/// <summary>
/// Builds one tree for a feature: the feature branch at the root, each plan's base
/// branch under the base branch of the plan done before it (or under the feature
/// branch), and each plan's own sub-branches beneath its base branch. Branches of
/// completed plans are already merged, so they are left out and the plans after
/// them move up: main &lt;- x1 &lt;- x2 shows as main &lt;- x2 once x1 is completed.
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
            [.. followers[null].SelectMany(plan => PlanNodes(plan, followers))]
        );

        return TextTree.Render(root);
    }

    private static List<TextTreeNode> PlanNodes(
        FeaturePlanDto plan,
        ILookup<Guid?, FeaturePlanDto> followers
    )
    {
        List<TextTreeNode> nextPlans =
            [.. followers[plan.Id].SelectMany(next => PlanNodes(next, followers))];

        if (plan.Status == FeaturePlanStatus.Completed)
            return nextPlans;

        var label = $"  [{TextTree.SingleLine(plan.Title)} · {StatusLabel(plan.Status.ToString())}]";

        return
        [
            plan.Branch is null
                ? new TextTreeNode("(no base branch)" + label, nextPlans)
                : FeaturePlanBranchTreeFormatter.ToNode(plan.Branch, nextPlans, label)
        ];
    }

    private static string StatusLabel(string status)
    {
        var words = WordStart().Replace(status, " $1").Trim().ToLowerInvariant();

        return char.ToUpperInvariant(words[0]) + words[1..];
    }

    [GeneratedRegex("([A-Z])")]
    private static partial Regex WordStart();
}
