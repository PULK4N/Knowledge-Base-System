using SkillsModule.Domain;

namespace SkillsModule.Persistence;

public static class SkillMarkdownCompiler
{
    public const string MainSkillPath = "SKILL.md";

    public static IReadOnlyList<SkillMarkdownSource> Compile(
        SkillStateData skill
    )
    {
        var tags = skill.Tags.Count == 0
            ? string.Empty
            : $"\n\nTags: {string.Join(", ", skill.Tags)}";
        var main = $"# {skill.Name}\n\n{skill.Description}{tags}\n\n{skill.Content}";
        var sources = new List<SkillMarkdownSource>
        {
            new(MainSkillPath, main.Trim())
        };
        sources.AddRange(
            skill.References
                .OrderBy(reference => reference.Key, StringComparer.Ordinal)
                .Select(
                    reference => new SkillMarkdownSource(
                        reference.Key,
                        reference.Value.Content.Trim()
                    )
                )
        );

        return sources;
    }
}

public sealed record SkillMarkdownSource(
    string RelativePath,
    string Markdown
);
