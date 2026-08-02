using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain.Models;

namespace PolicyModule.Domain.Events;

public interface ITopicPolicyAdded : IEvent;

public class TopicPolicyAddedV1(TopicName TopicName, Policy Policy) : ITopicPolicyAdded
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var generalPoliciesStateData = (GeneralPoliciesStateData)stateData;

        generalPoliciesStateData.Topics[TopicName].Policies.Add(Policy.PolicyId, Policy);

        return generalPoliciesStateData;
    }
}
