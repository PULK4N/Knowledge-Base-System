using SkillsModule.Application.DTOs;

namespace SkillsModule.MCP;

internal static class SkillTextFormatter
{
    public static string Format(SkillDto skill)
    {
        var tags = skill.Tags.Count == 0
            ? string.Empty
            : $"\n\nTags: {string.Join(", ", skill.Tags)}";
        var sections = new List<string>
        {
            $"# {skill.Name}\n\n{skill.Description}{tags}\n\n## Skill content\n\n{skill.Content}"
        };

        sections.AddRange(
            skill.References
                .OrderBy(reference => reference.Key, StringComparer.Ordinal)
                .Select(reference =>
                    $"## Reference: {reference.Key}\n\n{reference.Value.Content}")
        );

        if (skill.OtherReferences.Count > 0)
        {
            sections.Add(
                "## Other references\n\n"
                + string.Join(
                    "\n",
                    skill.OtherReferences.Select(reference => $"- {reference}")
                )
            );
        }

        return string.Join("\n\n", sections);
    }

    public static string FormatReference(
        string relativePath,
        SkillReferenceDto reference
    ) =>
        $"# Reference: {relativePath}\n\n{reference.Content}";
}
