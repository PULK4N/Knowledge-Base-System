using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using SkillsModule.Domain.Models;

namespace SkillsModule.Domain.Events;

public interface ISkillDeleted : IEvent;

public readonly record struct SkillDeletedV1 : ISkillDeleted
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (SkillStateData)stateData;
        state.IsDeleted = true;
        return state;
    }
}

public readonly record struct SkillDeletedV2(
    Guid SessionId,
    AggregateId MemoryAggregateId
) : ISkillDeleted
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (SkillStateData)stateData;
        state.IsDeleted = true;
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
