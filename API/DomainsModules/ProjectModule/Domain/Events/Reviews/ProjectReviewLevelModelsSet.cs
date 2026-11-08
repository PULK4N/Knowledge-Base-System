using System.Collections.Immutable;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using ProjectModule.Domain.Models;

namespace ProjectModule.Domain.Events;

public interface IProjectReviewLevelModelsSet : IEvent;

public readonly record struct ProjectReviewLevelModelsSetV1(
    ImmutableDictionary<ReviewLevel, ReviewModel> LevelModels,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IProjectReviewLevelModelsSet
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var project = (ProjectStateData)stateData;

        project.ReviewLevelModels = LevelModels.ToDictionary();
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
