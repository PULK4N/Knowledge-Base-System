using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using ProjectModule.Domain.Models;

namespace ProjectModule.Domain.Events;

public interface IProjectProposalNoteRemoved : IEvent;

public readonly record struct ProjectProposalNoteRemovedV1(
    ProposalNoteId NoteId,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IProjectProposalNoteRemoved
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var project = (ProjectStateData)stateData;

        project.ProposalNotes.Remove(NoteId);
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
