using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using SkillsModule.Domain.Models;

namespace SkillsModule.Domain.Events;

public interface ISkillReferenceDeleted : IEvent;

public readonly record struct SkillReferenceDeletedV1(
    string RelativePath
) : ISkillReferenceDeleted
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (SkillStateData)stateData;
        state.References.Remove(RelativePath);

        return state;
    }
}

public readonly record struct SkillReferenceDeletedV2(
    string RelativePath,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : ISkillReferenceDeleted
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (SkillStateData)stateData;
        state.References.Remove(RelativePath);
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
