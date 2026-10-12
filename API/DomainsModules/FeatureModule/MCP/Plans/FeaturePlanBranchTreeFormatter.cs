using FeatureModule.Application.DTOs;

namespace FeatureModule.MCP;

public static class FeaturePlanBranchTreeFormatter
{
    public static string? Format(FeaturePlanBranchDto? branch) =>
        branch is null ? null : TextTree.Render(ToNode(branch, []));

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

    public static string BranchLine(string name, string description)
    {
        var singleLine = TextTree.SingleLine(description);

        return singleLine.Length == 0 ? name : $"{name} - {singleLine}";
    }
}
