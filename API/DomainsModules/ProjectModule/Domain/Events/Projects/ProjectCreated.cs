using System.Collections.Immutable;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;


namespace ProjectModule.Domain.Events;

public interface IProjectCreated : IEvent;

public readonly record struct ProjectCreatedV1(
    string ProjectName,
    string ProjectDescription,
    ImmutableArray<string> RepositoryPaths
) : IProjectCreated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var project = (ProjectStateData)stateData;
        project.ProjectName = ProjectName;
        project.ProjectDescription = ProjectDescription;
        project.RepositoryPaths = RepositoryPaths.ToList();

        return project;
    }
}

public readonly record struct ProjectCreatedV2(
    string ProjectName,
    string ProjectDescription,
    ImmutableArray<string> RepositoryPaths,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IProjectCreated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var project = (ProjectStateData)stateData;
        project.ProjectName = ProjectName;
        project.ProjectDescription = ProjectDescription;
        project.RepositoryPaths = RepositoryPaths.ToList();

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
