using EventSourcing.Shared.Models;
using SkillsModule.Domain.Events;

namespace SkillsModule.Domain.Constraints;

public sealed class UniqueSkillNameConstraint : IUniqueConstraintCreator<SkillStateData>
{
    public const string ConstraintName = "skill-name";

    public IEnumerable<UniqueEventConstraintData> CreateConstraintsToRemove(
        SkillStateData stateBeforeEvent,
        EventPayload payload
    )
    {
        var currentName = Normalize(stateBeforeEvent.Skill.Name);

        if (stateBeforeEvent.IsDeleted || currentName is null)
            return [];

        if (payload.EventData is SkillSaved)
            return [];

        var updatedName = payload.EventData switch
        {
            SkillUpdated updated => updated.Name,
            SkillDetailsUpdated updated => updated.Name,
            _ => null
        };

        if (updatedName is not null && currentName == Normalize(updatedName))
            return [];

        return (
            payload.EventData is SkillUpdated
                or SkillDetailsUpdated
                or SkillDeleted
        )
            ? [CreateConstraint(currentName)]
            : [];
    }

    public IEnumerable<UniqueEventConstraintData> CreateConstraintsToAdd(
        SkillStateData stateAfterEvent,
        EventPayload payload
    )
    {
        var currentName = Normalize(stateAfterEvent.Skill.Name);

        if (stateAfterEvent.IsDeleted || currentName is null)
            return [];

        if (payload.EventData is SkillSaved)
            return [CreateConstraint(currentName)];

        if (
            (payload.EventData is SkillUpdated or SkillDetailsUpdated)
            && payload.UniqueEventConstraintsToRemove.Any(
                constraint => constraint.ConstraintName == ConstraintName
            )
        )
            return [CreateConstraint(currentName)];

        return [];
    }

    private static UniqueEventConstraintData CreateConstraint(string normalizedName) =>
        new(ConstraintName, normalizedName);

    private static string? Normalize(string name)
    {
        var normalizedName = name.Trim();

        return normalizedName.Length == 0
            ? null
            : normalizedName.ToUpperInvariant();
    }
}
