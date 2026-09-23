using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

using PolicyModule.Domain.Models;

namespace PolicyModule.Domain.Events;

public interface IProjectDeleted : IEvent;

public readonly record struct ProjectDeletedV1 : IProjectDeleted
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var projectPolicies = (ProjectPoliciesStateData)stateData;
        projectPolicies.IsDeleted = true;

        return projectPolicies;
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
        var projectPolicies = (ProjectPoliciesStateData)stateData;
        projectPolicies.IsDeleted = true;

        projectPolicies.MemoryHistory.Add(
            new MemoryHistoryRecord(
                eventExecutionInfo.EventName,
                eventExecutionInfo.Timestamp,
                MemoryAggregateId
            )
        );
        return projectPolicies;
    }
}
