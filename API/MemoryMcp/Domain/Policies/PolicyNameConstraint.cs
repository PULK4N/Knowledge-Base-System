using EventSourcing.Shared.Models;

namespace MemoryMcp.Domain.Policies;

public sealed class PolicyNameConstraint : IUniqueConstraintCreator<PolicyState>
{
    public IEnumerable<UniqueEventConstraintData> CreateConstraintsToRemove(
        PolicyState stateBeforeEvent,
        EventPayload payload
    )
    {
        if (payload.EventData is PolicyDeleted)
            return One(stateBeforeEvent.Name);

        if (
            payload.EventData is PolicyUpdated updated
            && Normalize(updated.PreviousName) != Normalize(updated.Name)
        )
            return One(stateBeforeEvent.Name);

        return [ ];
    }

    public IEnumerable<UniqueEventConstraintData> CreateConstraintsToAdd(
        PolicyState stateAfterEvent,
        EventPayload payload
    )
    {
        if (payload.EventData is PolicySaved)
            return One(stateAfterEvent.Name);

        if (
            payload.EventData is PolicyUpdated updated
            && Normalize(updated.PreviousName) != Normalize(updated.Name)
        )
            return One(stateAfterEvent.Name);

        return [ ];
    }

    private static UniqueEventConstraintData[] One(string name) =>
        [ new(nameof(PolicyNameConstraint), Normalize(name)) ];

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
}
