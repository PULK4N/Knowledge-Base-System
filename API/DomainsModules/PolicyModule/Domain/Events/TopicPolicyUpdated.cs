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
