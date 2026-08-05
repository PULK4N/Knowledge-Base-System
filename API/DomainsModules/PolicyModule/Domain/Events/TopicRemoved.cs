using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain.Models;

namespace PolicyModule.Domain.Events;

public interface ITopicRemoved : IEvent;

public readonly record struct TopicRemovedV1(
    TopicName TopicName
) : ITopicRemoved
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var generalPolicies = (GeneralPoliciesStateData)stateData;
        generalPolicies.Topics.Remove(TopicName);

        return generalPolicies;
    }
}
