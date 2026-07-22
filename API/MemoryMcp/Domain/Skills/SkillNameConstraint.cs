using EventSourcing.Shared.Models;

namespace MemoryMcp.Domain.Skills;

public sealed class SkillNameConstraint : IUniqueConstraintCreator<SkillState>
{
    public IEnumerable<UniqueEventConstraintData> CreateConstraintsToRemove(
        SkillState stateBeforeEvent,
        EventPayload payload
    )
    {
        if (payload.EventData is SkillDeleted)
            return One(stateBeforeEvent.Name);

        if (
            payload.EventData is SkillUpdated updated
            && Normalize(updated.PreviousName) != Normalize(updated.Name)
        )
            return One(stateBeforeEvent.Name);

        return [ ];
    }

    public IEnumerable<UniqueEventConstraintData> CreateConstraintsToAdd(
        SkillState stateAfterEvent,
        EventPayload payload
    )
    {
        if (payload.EventData is SkillSaved)
            return One(stateAfterEvent.Name);

        if (
            payload.EventData is SkillUpdated updated
            && Normalize(updated.PreviousName) != Normalize(updated.Name)
        )
            return One(stateAfterEvent.Name);

        return [ ];
    }

    private static UniqueEventConstraintData[] One(string name) =>
        [ new(nameof(SkillNameConstraint), Normalize(name)) ];

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
}
