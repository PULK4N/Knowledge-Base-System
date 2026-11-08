using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain.Models;

namespace PolicyModule.Domain.Events;

public interface IAgentFamilyPolicyAdded : IEvent;

public readonly record struct AgentFamilyPolicyAddedV1(
    AgentFamilyName AgentFamilyName,
    Policy Policy
) : IAgentFamilyPolicyAdded
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var generalPolicies = (GeneralPoliciesStateData)stateData;
        generalPolicies.AgentFamilies[AgentFamilyName].UpdatedAt = eventExecutionInfo.Timestamp;
        generalPolicies.AgentFamilies[AgentFamilyName].Policies.Add(
            Policy.PolicyId,
            Policy with { UpdatedAt = eventExecutionInfo.Timestamp }
        );

        return generalPolicies;
    }
}

public readonly record struct AgentFamilyPolicyAddedV2(
    AgentFamilyName AgentFamilyName,
    Policy Policy,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IAgentFamilyPolicyAdded
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var generalPolicies = (GeneralPoliciesStateData)stateData;
        generalPolicies.AgentFamilies[AgentFamilyName].UpdatedAt = eventExecutionInfo.Timestamp;
        generalPolicies.AgentFamilies[AgentFamilyName].Policies.Add(
            Policy.PolicyId,
            Policy with { UpdatedAt = eventExecutionInfo.Timestamp }
        );

        generalPolicies.MemoryHistory.Add(
            new MemoryHistoryRecord(
                eventExecutionInfo.EventName,
                eventExecutionInfo.Timestamp,
                MemoryAggregateId,
                Policy.PolicyId
            )
        );
        return generalPolicies;
    }
}
