using System.Text.RegularExpressions;
using FeatureModule.Application.DTOs;
using FeatureModule.Domain.Models;

namespace FeatureModule.MCP;

/// <summary>
/// Writes the feature's branch graph as one line per branch with the branches
/// it came from. A branch can come from several branches, which a tree cannot
/// show, so a flat list is used:
/// <code>
/// main (feature branch)
/// x1-api  [Plan X1 · In progress]  from: main
/// x1-web  [Plan X1 · In progress]  from: main
/// x2  [Plan X2 · Not started]  from: x1-api, x1-web
/// </code>
/// Branches of completed plans are already left out of the graph.
/// </summary>
public static partial class FeatureBranchGraphFormatter
{
    private const string FeatureBranchLabel = "feature branch";

    public static string? Format(FeatureDto feature)
    {
        if (feature.Branch is null && feature.BranchGraph.Count <= 1)
            return null;

        var names = feature.BranchGraph.ToDictionary(
            node => node.Key,
            node => node.Name ?? FeatureBranchLabel
        );

        return string.Join(
            '\n',
            feature.BranchGraph.Select(node => Line(node, names))
        );
    }

    public static string StatusLabel(FeaturePlanStatus status)
    {
        var words = WordStart().Replace(status.ToString(), " $1").Trim().ToLowerInvariant();

        return char.ToUpperInvariant(words[0]) + words[1..];
    }

    private static string Line(
        FeatureBranchGraphNodeDto node,
        Dictionary<string, string> names
    )
    {
        if (node.PlanId is null)
        {
            return node.Name is null
                ? "(feature branch not set)"
                : $"{FeaturePlanBranchTreeFormatter.BranchLine(node.Name, node.Description)}  ({FeatureBranchLabel})";
        }

        var plan = node.IsBaseBranch
            ? $"{TextTree.SingleLine(node.PlanTitle ?? string.Empty)} · {StatusLabel(node.PlanStatus!.Value)}"
            : $"{TextTree.SingleLine(node.PlanTitle ?? string.Empty)} · sub-branch";
        var from = string.Join(", ", node.FromKeys.Select(key => names[key]));

        return $"{FeaturePlanBranchTreeFormatter.BranchLine(node.Name!, node.Description)}  [{plan}]  from: {from}";
    }

    [GeneratedRegex("([A-Z])")]
    private static partial Regex WordStart();
}
