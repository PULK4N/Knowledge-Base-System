using System.Text.RegularExpressions;

namespace SkillsModule.Persistence;

public static partial class MarkdownChunker
{
    public const int MaximumChunkLength = 2000;
    public const int ChunkOverlapLength = 500;

    public static IReadOnlyList<string> Split(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return [];

        var normalized = markdown
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Trim();
        var sections = HeadingBoundary()
            .Split(normalized)
            .Where(section => !string.IsNullOrWhiteSpace(section))
            .Select(section => section.Trim())
            .ToList();

        return sections
            .SelectMany(SplitOversizedSection)
            .ToList();
    }

    private static IEnumerable<string> SplitOversizedSection(
        string section
    )
    {
        if (section.Length <= MaximumChunkLength)
            return [section];

        var chunks = new List<string>();
        var step = MaximumChunkLength - ChunkOverlapLength;

        for (var offset = 0; offset < section.Length; offset += step)
        {
            var length = Math.Min(
                MaximumChunkLength,
                section.Length - offset
            );
            chunks.Add(section.Substring(offset, length));

            if (offset + length == section.Length)
                break;
        }

        return chunks;
    }

    [GeneratedRegex(@"(?=^#{1,6}[ \t]+)", RegexOptions.Multiline)]
    private static partial Regex HeadingBoundary();
}
