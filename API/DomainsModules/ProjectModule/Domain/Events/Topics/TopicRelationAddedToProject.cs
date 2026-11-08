using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace ProjectModule.Domain.Events;

public interface ITopicRelationAddedToProject : IEvent;

public readonly record struct TopicRelationAddedToProjectV1(TopicName TopicName)
    : ITopicRelationAddedToProject
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var projectStateData = (ProjectStateData)stateData;

        projectStateData.RelatedTopics.Add(TopicName);

        return projectStateData;
    }
}

public readonly record struct TopicRelationAddedToProjectV2(
    TopicName TopicName,
    Guid SessionId,
    AggregateId MemoryAggregateId
)
    : ITopicRelationAddedToProject
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var projectStateData = (ProjectStateData)stateData;

        projectStateData.RelatedTopics.Add(TopicName);

        projectStateData.MemoryHistory.Add(
            new MemoryHistoryRecord(
                eventExecutionInfo.EventName,
                eventExecutionInfo.Timestamp,
                MemoryAggregateId
            )
        );
        return projectStateData;
    }
}
