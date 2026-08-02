using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain.Models;

namespace PolicyModule.Domain.Events;

public interface ITopicRelationAddedToProject : IEvent;

public readonly record struct TopicRelationAddedToProjectV1(TopicName TopicName)
    : ITopicRelationAddedToProject
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var projectPoliciesStateData = (ProjectPoliciesStateData)stateData;

        projectPoliciesStateData.RelatedTopics.Add(TopicName);

        return projectPoliciesStateData;
    }
}
