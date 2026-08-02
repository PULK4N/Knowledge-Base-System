using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain.Models;

namespace PolicyModule.Domain.Events;

public interface ITopicCreated : IEvent;

public readonly record struct TopicCreatedV1(Topic Topic) : ITopicCreated
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var generalPoliciesStateData = (GeneralPoliciesStateData)stateData;
        generalPoliciesStateData.Topics.Add(Topic.TopicName, Topic);

        return generalPoliciesStateData;
    }
}
