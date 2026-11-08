using EventSourcing.Shared.Models;
using ProjectModule.Domain.Events;
using Shared.Interfaces;

namespace ProjectModule.Domain.Validators;

public sealed class ProjectMustNotExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var exists = !string.IsNullOrWhiteSpace(
            ((ProjectStateData)stateData).ProjectName
        );

        return EventValidationResult.FromPayload(
            payload,
            nameof(ProjectMustNotExistValidator),
            !exists,
            exists ? "The project already exists." : null
        );
    }
}
