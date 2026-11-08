using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using ProjectModule.Domain.Models;

namespace ProjectModule.Domain.Events;

public interface IProjectSkillRelationAdded : IEvent;

public readonly record struct ProjectSkillRelationAddedV1(
    AggregateId SkillId,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IProjectSkillRelationAdded
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var project = (ProjectStateData)stateData;

        project.RelatedSkillIds.Add(SkillId);
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

public readonly record struct ProjectSkillRelationAddedV2(
    AggregateId SkillId,
    bool UsedForReview,
    string ReviewNotes,
    ReviewLevel? ReviewLevel,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IProjectSkillRelationAdded
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var project = (ProjectStateData)stateData;

        project.RelatedSkillIds.Add(SkillId);
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
