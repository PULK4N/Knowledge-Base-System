using System.Collections.Immutable;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using SkillsModule.Domain.Models;

namespace SkillsModule.Domain.Events;

public interface ISkillDetailsUpdated : IEvent;

public readonly record struct SkillDetailsUpdatedV1(
    string Name,
    string Description,
    string Content,
    ImmutableArray<string> Tags
) : ISkillDetailsUpdated
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (SkillStateData)stateData;
        state.Name = Name;
        state.Description = Description;
        state.Content = Content;
        state.Tags = Tags.ToList();
        return state;
    }
}

public readonly record struct SkillDetailsUpdatedV2(
    string Name,
    string Description,
    string Content,
    ImmutableArray<string> Tags,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : ISkillDetailsUpdated
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (SkillStateData)stateData;
        state.Name = Name;
        state.Description = Description;
        state.Content = Content;
        state.Tags = Tags.ToList();
        state.MemoryHistory.Add(
            new MemoryHistoryRecord(
                eventExecutionInfo.EventName,
                eventExecutionInfo.Timestamp,
                MemoryAggregateId
            )
        );

        return state;
    }
}
