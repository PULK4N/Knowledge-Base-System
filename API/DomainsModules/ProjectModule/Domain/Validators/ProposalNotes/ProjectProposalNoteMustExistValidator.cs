using EventSourcing.Shared.Models;
using ProjectModule.Domain.Events;
using Shared.Interfaces;

namespace ProjectModule.Domain.Validators;

public sealed class ProjectProposalNoteMustExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var noteId = payload.EventData switch
        {
            ProjectProposalNoteUpdatedV1 data => data.NoteId,
            ProjectProposalNoteRemovedV1 data => data.NoteId,
            _ => throw new InvalidCastException()
        };
        var exists = ((ProjectStateData)stateData).ProposalNotes.ContainsKey(noteId);

        return EventValidationResult.FromPayload(
            payload,
            nameof(ProjectProposalNoteMustExistValidator),
            exists,
            exists ? null : $"Proposal note '{noteId.Value}' does not exist."
        );
    }
}
