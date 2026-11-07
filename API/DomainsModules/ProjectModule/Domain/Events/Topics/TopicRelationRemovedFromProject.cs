using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace ProjectModule.Domain.Events;

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
        var project = (ProjectStateData)stateData;
        project.RelatedTopics.Remove(TopicName);

        return project;
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
        var project = (ProjectStateData)stateData;
        project.RelatedTopics.Remove(TopicName);

        project.MemoryHistory.Add(
            new MemoryHistoryRecord(
                eventExecutionInfo.EventName,
                eventExecutionInfo.Timestamp,
                MemoryAggregateId
            )
        );
        return project;
    }
}
