using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain.Models;

namespace PolicyModule.Domain.Events;

public interface ITopicUpdated : IEvent;

public readonly record struct TopicUpdatedV1(
    TopicName TopicName,
    string Description
) : ITopicUpdated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var generalPolicies = (GeneralPoliciesStateData)stateData;
        var existingTopic = generalPolicies.Topics[TopicName];
        var updatedTopic = new Topic
        {
            TopicName = TopicName,
            Description = Description
        };

        foreach (var policy in existingTopic.Policies)
            updatedTopic.Policies.Add(policy.Key, policy.Value);

        generalPolicies.Topics[TopicName] = updatedTopic;

        return generalPolicies;
    }
}
