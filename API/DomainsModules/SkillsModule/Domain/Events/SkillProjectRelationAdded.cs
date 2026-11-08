using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using SkillsModule.Domain.Models;

namespace SkillsModule.Domain.Events;

public interface ISkillProjectRelationAdded : IEvent;

public readonly record struct SkillProjectRelationAddedV1(
    AggregateId ProjectId,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : ISkillProjectRelationAdded
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (SkillStateData)stateData;
        state.RelatedProjectIds.Add(ProjectId);
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
