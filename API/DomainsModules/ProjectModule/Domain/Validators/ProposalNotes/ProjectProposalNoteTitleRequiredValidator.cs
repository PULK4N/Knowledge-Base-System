using EventSourcing.Shared.Models;
using ProjectModule.Domain.Events;
using Shared.Interfaces;

namespace ProjectModule.Domain.Validators;

public sealed class ProjectProposalNoteTitleRequiredValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var title = payload.EventData switch
        {
            ProjectProposalNoteAddedV1 data => data.Title,
            ProjectProposalNoteUpdatedV1 data => data.Title,
            _ => throw new InvalidCastException()
        };
        var valid = !string.IsNullOrWhiteSpace(title);

        return EventValidationResult.FromPayload(
            payload,
            nameof(ProjectProposalNoteTitleRequiredValidator),
            valid,
            valid ? null : "A proposal note needs a title."
        );
    }
}
