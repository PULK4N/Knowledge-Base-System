using MemoryModule.Domain.Models;
using SharedModule.Constants;

namespace MemoryModule.Persistence;

/// <summary>
/// Turns the attachments of a prompt into Markdown links inside the prompt
/// text, so readers that render messages as Markdown show pasted images where
/// the user placed them.
/// </summary>
public static class MemoryAttachmentLinks
{
    /// <summary>
    /// Replaces each attachment placeholder found in the text with a link to
    /// its content. When <paramref name="appendUnplaced"/> is set, attachments
    /// whose placeholder is missing from the text are appended after it, so
    /// they are shown once even without a marker.
    /// </summary>
    public static string Apply(
        string text,
        IReadOnlyCollection<MemoryAttachment> attachments,
        bool appendUnplaced
    )
    {
        var placeholders = new HashSet<string>(StringComparer.Ordinal);
        var unplaced = new List<string>();

        foreach (var attachment in attachments)
        {
            if (attachment.Placeholder is { } placeholder)
            {
                // A retried upload can record the same placeholder twice; the
                // first attachment already stands for it.
                if (!placeholders.Add(placeholder))
                    continue;

                if (text.Contains(placeholder, StringComparison.Ordinal))
                {
                    text = text.Replace(
                        placeholder,
                        ToMarkdown(attachment),
                        StringComparison.Ordinal
                    );
                    continue;
                }
            }

            unplaced.Add(ToMarkdown(attachment));
        }

        if (!appendUnplaced || unplaced.Count == 0)
            return text;

        return string.IsNullOrWhiteSpace(text)
            ? string.Join("\n\n", unplaced)
            : string.Join("\n\n", unplaced.Prepend(text));
    }

    private static string ToMarkdown(MemoryAttachment attachment)
    {
        var url = AttachmentRoutes.Content(attachment.Id.Value);
        var label = WithoutBrackets(
            attachment.Placeholder ?? attachment.Name
        );

        return IsImage(attachment.FileType)
            ? $"![{label}]({url})"
            : $"[{label}]({url})";
    }

    /// <summary>
    /// SVG is excluded because the attachment endpoint only serves raster
    /// images inline; an SVG link would not render as an image.
    /// </summary>
    private static bool IsImage(string fileType) =>
        fileType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
        && !fileType.Equals("image/svg+xml", StringComparison.OrdinalIgnoreCase);

    private static string WithoutBrackets(string text) =>
        text.Trim().TrimStart('[').TrimEnd(']').Replace("[", "(").Replace("]", ")");
}
