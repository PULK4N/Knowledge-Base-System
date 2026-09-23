using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain.Models;

namespace PolicyModule.Domain.Events;

public interface ITopicPolicyAdded : IEvent;

public readonly record struct TopicPolicyAddedV1(TopicName TopicName, Policy Policy) : ITopicPolicyAdded
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var generalPoliciesStateData = (GeneralPoliciesStateData)stateData;

        generalPoliciesStateData.Topics[TopicName].Policies.Add(Policy.PolicyId, Policy);

        return generalPoliciesStateData;
    }
}

public readonly record struct TopicPolicyAddedV2(
    TopicName TopicName, Policy Policy,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : ITopicPolicyAdded
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var generalPoliciesStateData = (GeneralPoliciesStateData)stateData;

        generalPoliciesStateData.Topics[TopicName].Policies.Add(Policy.PolicyId, Policy);

        generalPoliciesStateData.MemoryHistory.Add(
            new MemoryHistoryRecord(
                eventExecutionInfo.EventName,
                eventExecutionInfo.Timestamp,
                MemoryAggregateId,
                Policy.PolicyId
            )
        );
        return generalPoliciesStateData;
    }
}
