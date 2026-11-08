using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;
using ProjectModule.Shared;

namespace FeatureModule.Domain.Events;

public interface IFeatureReviewerAdded : IEvent;

public sealed record FeatureReviewerAddedV1(
    FeatureReviewerId ReviewerId,
    string Title,
    string Description,
    ReviewLevel ReviewLevel,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeatureReviewerAdded
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (FeatureStateData)stateData;
        state.Reviewers.Add(new FeatureReviewer
        {
            Id = ReviewerId,
            Title = Title,
            Description = Description,
            ReviewLevel = ReviewLevel,
            CreatedAt = eventExecutionInfo.Timestamp,
            UpdatedAt = eventExecutionInfo.Timestamp
        });
        state.MemoryHistory.Add(new MemoryHistoryRecord(
            eventExecutionInfo.EventName, eventExecutionInfo.Timestamp, MemoryAggregateId));
        return state;
    }
}
