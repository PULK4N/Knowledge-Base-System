using EventSourcing.Shared.Models;
using ProjectModule.Domain.Events;
using Shared.Interfaces;

namespace ProjectModule.Domain.Validators;

public sealed class ProjectProposalNoteMustNotExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var noteId = payload.EventData switch
        {
            ProjectProposalNoteAddedV1 data => data.NoteId,
            _ => throw new InvalidCastException()
        };
        var exists = ((ProjectStateData)stateData).ProposalNotes.ContainsKey(noteId);

        return EventValidationResult.FromPayload(
            payload,
            nameof(ProjectProposalNoteMustNotExistValidator),
            !exists,
            exists ? $"Proposal note '{noteId.Value}' already exists." : null
        );
    }
}
