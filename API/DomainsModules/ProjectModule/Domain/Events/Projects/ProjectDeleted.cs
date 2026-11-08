using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;


namespace ProjectModule.Domain.Events;

public interface IProjectDeleted : IEvent;

public readonly record struct ProjectDeletedV1 : IProjectDeleted
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var project = (ProjectStateData)stateData;
        project.IsDeleted = true;

        return project;
    }
}

public readonly record struct ProjectDeletedV2(
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IProjectDeleted
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var project = (ProjectStateData)stateData;
        project.IsDeleted = true;

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
