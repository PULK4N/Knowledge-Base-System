using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain.Models;

namespace PolicyModule.Domain.Events;

public interface ITopicRelationRemovedFromProject : IEvent;

public readonly record struct TopicRelationRemovedFromProjectV1(
    TopicName TopicName
) : ITopicRelationRemovedFromProject
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var projectPolicies = (ProjectPoliciesStateData)stateData;
        projectPolicies.RelatedTopics.Remove(TopicName);

        return projectPolicies;
    }
}
