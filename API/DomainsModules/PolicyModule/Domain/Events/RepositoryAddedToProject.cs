using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

using PolicyModule.Domain.Models;

namespace PolicyModule.Domain.Events;

public interface IRepositoryAddedToProject : IEvent;

public readonly record struct RepositoryAddedToProjectV1(
    string RepositoryPath
) : IRepositoryAddedToProject
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var project = (ProjectPoliciesStateData)stateData;
        project.RepositoryPaths.Add(RepositoryPath);

        return project;
    }
}

public readonly record struct RepositoryAddedToProjectV2(
    string RepositoryPath,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IRepositoryAddedToProject
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var project = (ProjectPoliciesStateData)stateData;
        project.RepositoryPaths.Add(RepositoryPath);

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
