using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain.Models;

namespace PolicyModule.Domain.Events;

public interface ITopicPolicyRemoved : IEvent;

public readonly record struct TopicPolicyRemovedV1(TopicName TopicName, PolicyId PolicyId)
    : ITopicPolicyRemoved
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var generalPoliciesStateData = (GeneralPoliciesStateData)stateData;

        generalPoliciesStateData.Topics[TopicName].Policies.Remove(PolicyId);

        return generalPoliciesStateData;
    }
}

public readonly record struct TopicPolicyRemovedV2(
    TopicName TopicName, PolicyId PolicyId,
    Guid SessionId,
    AggregateId MemoryAggregateId
)
    : ITopicPolicyRemoved
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var generalPoliciesStateData = (GeneralPoliciesStateData)stateData;

        generalPoliciesStateData.Topics[TopicName].Policies.Remove(PolicyId);

        generalPoliciesStateData.MemoryHistory.Add(
            new MemoryHistoryRecord(
                eventExecutionInfo.EventName,
                eventExecutionInfo.Timestamp,
                MemoryAggregateId
            )
        );
        return generalPoliciesStateData;
    }
}
