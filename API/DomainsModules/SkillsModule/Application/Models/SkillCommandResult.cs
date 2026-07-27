namespace SkillsModule.Application.Models;

public sealed record SkillCommandResult(string Status)
{
    public static SkillCommandResult Ok { get; } = new("OK");
}

public sealed record SkillCreatedCommandResult(
    string Status,
    Guid SkillId
)
{
    public static SkillCreatedCommandResult Ok(Guid skillId) =>
        new("OK", skillId);
}
