using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using ProjectModule.Domain.Models;

namespace ProjectModule.Domain.Events;

public interface IProjectProposalNoteAdded : IEvent;

public readonly record struct ProjectProposalNoteAddedV1(
    ProposalNoteId NoteId,
    string Title,
    string Content,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IProjectProposalNoteAdded
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var project = (ProjectStateData)stateData;

        project.ProposalNotes[NoteId] = new ProposalNote
        {
            Id = NoteId,
            Title = Title,
            Content = Content,
            CreatedAt = eventExecutionInfo.Timestamp,
            UpdatedAt = eventExecutionInfo.Timestamp
        };
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
