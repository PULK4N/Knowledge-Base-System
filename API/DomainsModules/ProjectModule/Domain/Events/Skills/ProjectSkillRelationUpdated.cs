using ProjectModule.Shared;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using ProjectModule.Domain.Models;

namespace ProjectModule.Domain.Events;

public interface IProjectSkillRelationUpdated : IEvent;

public readonly record struct ProjectSkillRelationUpdatedV1(
    AggregateId SkillId,
    bool UsedForReview,
    string ReviewNotes,
    ReviewLevel? ReviewLevel,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IProjectSkillRelationUpdated
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var project = (ProjectStateData)stateData;

        project.SkillReviews[SkillId] = new ProjectSkillReview(
            UsedForReview,
            ReviewNotes,
            ReviewLevel
        );
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
