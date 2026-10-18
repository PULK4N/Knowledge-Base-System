using EventSourcing.Shared.Interfaces;

namespace SkillsModule.Domain.Constraints;

public sealed class SkillNameConstraintCreator
    : IUniqueConstraintCreator<SkillStateData, UniqueSkillNameConstraint>
{
    public List<UniqueSkillNameConstraint> Create(SkillStateData stateData)
    {
        var normalizedName = Normalize(stateData.Name);

        return normalizedName is null
            ? []
            : [new UniqueSkillNameConstraint(stateData, normalizedName)];
    }

    private static string? Normalize(string name)
    {
        var normalizedName = name.Trim();

        return normalizedName.Length == 0
            ? null
            : normalizedName.ToUpperInvariant();
    }
}
