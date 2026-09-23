using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain.Models;

namespace PolicyModule.Domain.Events;

public interface IAgentFamilyPolicyUpdated : IEvent;

public readonly record struct AgentFamilyPolicyUpdatedV1(
    AgentFamilyName AgentFamilyName,
    Policy Policy
) : IAgentFamilyPolicyUpdated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var generalPolicies = (GeneralPoliciesStateData)stateData;
        generalPolicies.AgentFamilies[AgentFamilyName].Policies[
            Policy.PolicyId
        ] = Policy;

        return generalPolicies;
    }
}

public readonly record struct AgentFamilyPolicyUpdatedV2(
    AgentFamilyName AgentFamilyName,
    Policy Policy,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IAgentFamilyPolicyUpdated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var generalPolicies = (GeneralPoliciesStateData)stateData;
        generalPolicies.AgentFamilies[AgentFamilyName].Policies[
            Policy.PolicyId
        ] = Policy;

        generalPolicies.MemoryHistory.Add(
            new MemoryHistoryRecord(
                eventExecutionInfo.EventName,
                eventExecutionInfo.Timestamp,
                MemoryAggregateId
            )
        );
        return generalPolicies;
    }
}
