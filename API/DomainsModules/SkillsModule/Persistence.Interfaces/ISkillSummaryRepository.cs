namespace SkillsModule.Persistence.Interfaces;

public interface ISkillSummaryRepository
{
    Task<List<SkillSummary>> List();
}

public sealed record SkillSummary(
    Guid SkillId,
    string Name
);
