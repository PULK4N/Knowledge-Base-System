using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using ProjectModule.Domain.Models;

namespace ProjectModule.Domain.Events;

public interface IProjectProposalNoteUpdated : IEvent;

public readonly record struct ProjectProposalNoteUpdatedV1(
    ProposalNoteId NoteId,
    string Title,
    string Content,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IProjectProposalNoteUpdated
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var project = (ProjectStateData)stateData;
        var note = project.ProposalNotes[NoteId];

        note.Title = Title;
        note.Content = Content;
        note.UpdatedAt = eventExecutionInfo.Timestamp;
        project.MemoryHistory.Add(
            new MemoryHistoryRecord(
                eventExecutionInfo.EventName,
                eventExecutionInfo.Timestamp,
                MemoryAggregateId
            )
        );
        return project;
    }
}
