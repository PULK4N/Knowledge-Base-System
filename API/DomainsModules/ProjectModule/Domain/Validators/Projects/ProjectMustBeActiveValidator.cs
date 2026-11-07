using EventSourcing.Shared.Models;
using ProjectModule.Domain.Events;
using Shared.Interfaces;

namespace ProjectModule.Domain.Validators;

public sealed class ProjectMustBeActiveValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var state = (ProjectStateData)stateData;
        var exists = !string.IsNullOrWhiteSpace(state.ProjectName);
        var active = exists && !state.IsDeleted;
        var failureReason = !exists
            ? "The project does not exist."
            : state.IsDeleted
                ? "The project is deleted."
                : null;

        return EventValidationResult.FromPayload(
            payload,
            nameof(ProjectMustBeActiveValidator),
            active,
            failureReason
        );
    }
}
