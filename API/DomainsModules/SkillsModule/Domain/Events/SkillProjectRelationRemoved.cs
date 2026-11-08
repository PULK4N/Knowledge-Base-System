using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using SkillsModule.Domain.Models;

namespace SkillsModule.Domain.Events;

public interface ISkillProjectRelationRemoved : IEvent;

public readonly record struct SkillProjectRelationRemovedV1(
    AggregateId ProjectId,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : ISkillProjectRelationRemoved
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (SkillStateData)stateData;
        state.RelatedProjectIds.Remove(ProjectId);
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
