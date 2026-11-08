using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace ProjectModule.Domain.Events;

public interface IProjectSkillRelationRemoved : IEvent;

public readonly record struct ProjectSkillRelationRemovedV1(
    AggregateId SkillId,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IProjectSkillRelationRemoved
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var project = (ProjectStateData)stateData;

        project.RelatedSkillIds.Remove(SkillId);
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
