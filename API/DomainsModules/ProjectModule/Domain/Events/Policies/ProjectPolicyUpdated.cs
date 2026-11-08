using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace ProjectModule.Domain.Events;

public interface IProjectPolicyUpdated : IEvent;

public readonly record struct ProjectPolicyUpdatedV1(
    Policy Policy
) : IProjectPolicyUpdated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var project = (ProjectStateData)stateData;
        project.Policies[Policy.PolicyId] =
            Policy with { UpdatedAt = eventExecutionInfo.Timestamp };

        return project;
    }
}

public readonly record struct ProjectPolicyUpdatedV2(
    Policy Policy,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IProjectPolicyUpdated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var project = (ProjectStateData)stateData;
        project.Policies[Policy.PolicyId] =
            Policy with { UpdatedAt = eventExecutionInfo.Timestamp };

        project.MemoryHistory.Add(
            new MemoryHistoryRecord(
                eventExecutionInfo.EventName,
                eventExecutionInfo.Timestamp,
                MemoryAggregateId,
                Policy.PolicyId
            )
        );
        return project;
    }
}
