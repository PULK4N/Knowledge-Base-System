using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;

namespace FeatureModule.Domain.Constraints;

public sealed class UniqueFeatureNameConstraint
    : IUniqueConstraintCreator<FeatureStateData>
{
    public const string ConstraintName = "feature-name";

    public IEnumerable<UniqueEventConstraintData> CreateConstraintsToRemove(
        FeatureStateData stateBeforeEvent,
        EventPayload payload
    )
    {
        var currentName = Normalize(stateBeforeEvent.Name);

        if (
            stateBeforeEvent.IsDeleted
            || currentName is null
            || payload.EventData is FeatureAddedV1
        )
            return [];

        return (payload.EventData is FeatureRemovedV1)
            ? [CreateConstraint(currentName)]
            : [];
    }

    public IEnumerable<UniqueEventConstraintData> CreateConstraintsToAdd(
        FeatureStateData stateAfterEvent,
        EventPayload payload
    )
    {
        var currentName = Normalize(stateAfterEvent.Name);

        return (
            !stateAfterEvent.IsDeleted
                && currentName is not null
                && payload.EventData is FeatureAddedV1
        )
                ? [CreateConstraint(currentName)]
                : [];
    }

    private static UniqueEventConstraintData CreateConstraint(
        string normalizedName
    ) =>
        new(ConstraintName, normalizedName);

    private static string? Normalize(string name)
    {
        var normalizedName = name.Trim();

        return normalizedName.Length == 0
            ? null
            : normalizedName.ToUpperInvariant();
    }
}
