using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain.Models;

namespace PolicyModule.Domain.Events;

public interface ITopicPolicyUpdated : IEvent;

public readonly record struct TopicPolicyUpdatedV1(
    TopicName TopicName,
    Policy Policy
) : ITopicPolicyUpdated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var generalPolicies = (GeneralPoliciesStateData)stateData;
        generalPolicies.Topics[TopicName].Policies[
            Policy.PolicyId
        ] = Policy;

        return generalPolicies;
    }
}

public readonly record struct TopicPolicyUpdatedV2(
    TopicName TopicName,
    Policy Policy,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : ITopicPolicyUpdated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var generalPolicies = (GeneralPoliciesStateData)stateData;
        generalPolicies.Topics[TopicName].Policies[
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
