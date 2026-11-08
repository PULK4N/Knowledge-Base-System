using SkillsModule.Persistence.Interfaces;

namespace ProjectModule.Application;

/// <summary>
/// Builds the "# Project skills" section of the Agents.MD text, one line per
/// linked skill as "- name — scenario; scenario".
/// </summary>
public static class ProjectSkillsTextCompiler
{
    public static string Compile(List<SkillUseScenarios> skills)
    {
        if (skills.Count == 0)
            return string.Empty;

        var lines = skills.Select(
            skill => skill.UseScenarios.Count == 0
                ? $"- {skill.Name}"
                : $"- {skill.Name} — {string.Join("; ", skill.UseScenarios)}"
        );

        return $"# Project skills\n{string.Join("\n", lines)}";
    }
}
