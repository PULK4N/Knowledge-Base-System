using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;
using ProjectModule.Shared;

namespace FeatureModule.Domain.Events;

public interface IFeatureReviewerUpdated : IEvent;

public sealed record FeatureReviewerUpdatedV1(
    FeatureReviewerId ReviewerId,
    string Title,
    string Description,
    ReviewLevel ReviewLevel,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeatureReviewerUpdated
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (FeatureStateData)stateData;
        var reviewerId = ReviewerId;
        var reviewer = state.Reviewers.Single(item => item.Id == reviewerId);
        reviewer.Title = Title;
        reviewer.Description = Description;
        reviewer.ReviewLevel = ReviewLevel;
        reviewer.UpdatedAt = eventExecutionInfo.Timestamp;
        state.MemoryHistory.Add(new MemoryHistoryRecord(
            eventExecutionInfo.EventName, eventExecutionInfo.Timestamp, MemoryAggregateId));
        return state;
    }
}
