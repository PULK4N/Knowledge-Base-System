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
        var currentName = Normalize(stateBeforeEvent.Name);

        if (stateBeforeEvent.IsDeleted || currentName is null)
            return [];

        if (payload.EventData is ISkillCreated)
            return [];

        var updatedName = payload.EventData switch
        {
            ISkillUpdated updated => updated.Name,
            ISkillDetailsUpdated updated => updated.Name,
            _ => null
        };

        if (updatedName is not null && currentName == Normalize(updatedName))
            return [];

        return (
            payload.EventData is ISkillUpdated
                or ISkillDetailsUpdated
                or ISkillDeleted
        )
            ? [CreateConstraint(currentName)]
            : [];
    }

    public IEnumerable<UniqueEventConstraintData> CreateConstraintsToAdd(
        SkillStateData stateAfterEvent,
        EventPayload payload
    )
    {
        var currentName = Normalize(stateAfterEvent.Name);

        if (stateAfterEvent.IsDeleted || currentName is null)
            return [];

        if (payload.EventData is ISkillCreated)
            return [CreateConstraint(currentName)];

        if (
            (
                payload.EventData is ISkillUpdated
                    or ISkillDetailsUpdated
            )
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
