using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;


namespace ProjectModule.Domain.Events;

public interface IProjectUpdated : IEvent;

public readonly record struct ProjectUpdatedV1(
    string ProjectName,
    string ProjectDescription
) : IProjectUpdated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var project = (ProjectStateData)stateData;
        project.ProjectName = ProjectName;
        project.ProjectDescription = ProjectDescription;

        return project;
    }
}

public readonly record struct ProjectUpdatedV2(
    string ProjectName,
    string ProjectDescription,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IProjectUpdated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var project = (ProjectStateData)stateData;
        project.ProjectName = ProjectName;
        project.ProjectDescription = ProjectDescription;

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
