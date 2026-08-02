using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain.Models;

namespace PolicyModule.Domain.Events;

public interface ITopicCreated : IEvent;

public readonly record struct TopicCreatedV1(
    TopicName TopicName,
    string Description
) : ITopicCreated
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var generalPoliciesStateData = (GeneralPoliciesStateData)stateData;
        generalPoliciesStateData.Topics.Add(
            TopicName,
            new Topic
            {
                TopicName = TopicName,
                Description = Description
            }
        );

        return generalPoliciesStateData;
    }
}
