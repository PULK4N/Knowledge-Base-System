namespace SkillsModule.Persistence.Models;

public sealed class SkillSummaryEntry
{
    public int Id { get; set; }
    public required Guid SkillAggregateId { get; set; }
    public required string Name { get; set; }
}
