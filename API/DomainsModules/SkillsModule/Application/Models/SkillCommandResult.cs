namespace SkillsModule.Application.Models;

public sealed record SkillCommandResult(string Status)
{
    public static SkillCommandResult Ok { get; } = new("OK");
}
