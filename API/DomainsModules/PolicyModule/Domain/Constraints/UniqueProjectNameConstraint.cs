using EventSourcing.Shared.Models;
using PolicyModule.Domain.Events;

namespace PolicyModule.Domain.Constraints;

public sealed class UniqueProjectNameConstraint
    : IUniqueConstraintCreator<ProjectPoliciesStateData>
{
    public const string ConstraintName = "project-name";

    public IEnumerable<UniqueEventConstraintData> CreateConstraintsToRemove(
        ProjectPoliciesStateData stateBeforeEvent,
        EventPayload payload
    )
    {
        var currentName = Normalize(stateBeforeEvent.ProjectName);

        if (stateBeforeEvent.IsDeleted || currentName is null)
            return [];

        if (payload.EventData is (ProjectCreatedV1 or ProjectCreatedV2))
            return [];

        var updatedName = payload.EventData switch
        {
            ProjectUpdatedV1 updated => Normalize(updated.ProjectName),
            ProjectUpdatedV2 updated => Normalize(updated.ProjectName),
            _ => null
        };

        if (updatedName is not null && currentName == updatedName)
            return [];

        return (
            payload.EventData is ProjectUpdatedV1 or ProjectUpdatedV2 or ProjectDeletedV1 or ProjectDeletedV2
        )
            ? [CreateConstraint(currentName)]
            : [];
    }

    public IEnumerable<UniqueEventConstraintData> CreateConstraintsToAdd(
        ProjectPoliciesStateData stateAfterEvent,
        EventPayload payload
    )
    {
        var currentName = Normalize(stateAfterEvent.ProjectName);

        if (stateAfterEvent.IsDeleted || currentName is null)
            return [];

        if (payload.EventData is (ProjectCreatedV1 or ProjectCreatedV2))
            return [CreateConstraint(currentName)];

        if (
            payload.EventData is (ProjectUpdatedV1 or ProjectUpdatedV2)
            && payload.UniqueEventConstraintsToRemove.Any(
                constraint => constraint.ConstraintName == ConstraintName
            )
        )
            return [CreateConstraint(currentName)];

        return [];
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
