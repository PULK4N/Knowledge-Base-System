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
