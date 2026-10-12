using System.Text;
using FeatureModule.Application.DTOs;

namespace FeatureModule.MCP;

/// <summary>
/// Renders plan branches like the Linux <c>tree</c> command so MCP clients
/// that show tool results as text can read the branch hierarchy directly.
/// </summary>
public static class FeaturePlanBranchTreeFormatter
{
    public static string? Format(FeaturePlanBranchDto? branch)
    {
        if (branch is null)
            return null;

        var builder = new StringBuilder();
        builder.Append(FormatLine(branch));
        AppendSubBranches(builder, branch, string.Empty);
        return builder.ToString();
    }

    private static void AppendSubBranches(
        StringBuilder builder,
        FeaturePlanBranchDto branch,
        string indent
    )
    {
        for (var index = 0; index < branch.SubBranches.Count; index++)
        {
            var subBranch = branch.SubBranches[index];
            var isLast = index == branch.SubBranches.Count - 1;
            builder
                .Append('\n')
                .Append(indent)
                .Append(isLast ? "└── " : "├── ")
                .Append(FormatLine(subBranch));
            AppendSubBranches(
                builder,
                subBranch,
                indent + (isLast ? "    " : "│   ")
            );
        }
    }

    private static string FormatLine(FeaturePlanBranchDto branch)
    {
        var description = string.Join(
            ' ',
            branch.Description.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries
            )
        );

        return description.Length == 0
            ? branch.Name
            : $"{branch.Name} - {description}";
    }
}
