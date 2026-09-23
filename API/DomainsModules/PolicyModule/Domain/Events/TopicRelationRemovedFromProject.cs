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

public readonly record struct TopicRelationRemovedFromProjectV2(
    TopicName TopicName,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : ITopicRelationRemovedFromProject
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var projectPolicies = (ProjectPoliciesStateData)stateData;
        projectPolicies.RelatedTopics.Remove(TopicName);

        projectPolicies.MemoryHistory.Add(
            new MemoryHistoryRecord(
                eventExecutionInfo.EventName,
                eventExecutionInfo.Timestamp,
                MemoryAggregateId
            )
        );
        return projectPolicies;
    }
}
