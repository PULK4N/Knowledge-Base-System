using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain.Models;

namespace PolicyModule.Domain.Events;

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
        var projectPolicies = (ProjectPoliciesStateData)stateData;
        projectPolicies.Policies[Policy.PolicyId] = Policy;

        return projectPolicies;
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
        var projectPolicies = (ProjectPoliciesStateData)stateData;
        projectPolicies.Policies[Policy.PolicyId] = Policy;

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
