namespace ProjectModule.Application.Models;

public sealed record ProposalNoteAddedCommandResult(
    string Status,
    Guid NoteId
)
{
    public static ProposalNoteAddedCommandResult Ok(Guid noteId) =>
        new("OK", noteId);
}
