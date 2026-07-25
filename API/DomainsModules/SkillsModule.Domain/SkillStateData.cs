using EventSourcing.Shared.Models;
using SkillsModule.Domain.Models;

namespace SkillsModule.Domain;

public sealed class SkillStateData : ISharedStateData
{
    public AggregateId Id { get; set; }
    public bool IsDeleted { get; set; }
    public SkillDefinition Skill { get; set; } = new();
}
