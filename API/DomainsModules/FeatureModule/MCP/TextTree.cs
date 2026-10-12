using System.Text;

namespace FeatureModule.MCP;

public sealed record TextTreeNode(string Line, IReadOnlyList<TextTreeNode> Children);

/// <summary>
/// Renders nodes like the Linux <c>tree</c> command so MCP clients that show
/// tool results as text can read a hierarchy directly.
/// </summary>
public static class TextTree
{
    public static string Render(TextTreeNode root)
    {
        var builder = new StringBuilder(root.Line);
        AppendChildren(builder, root, string.Empty);
        return builder.ToString();
    }

    /// <summary>Joins whitespace and line breaks so each node stays on one line.</summary>
    public static string SingleLine(string text) =>
        string.Join(
            ' ',
            text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
        );

    private static void AppendChildren(
        StringBuilder builder,
        TextTreeNode node,
        string indent
    )
    {
        for (var index = 0; index < node.Children.Count; index++)
        {
            var child = node.Children[index];
            var isLast = index == node.Children.Count - 1;
            builder
                .Append('\n')
                .Append(indent)
                .Append(isLast ? "└── " : "├── ")
                .Append(child.Line);
            AppendChildren(builder, child, indent + (isLast ? "    " : "│   "));
        }
    }
}
