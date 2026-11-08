using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;
using ProjectModule.Shared;

namespace FeatureModule.Domain.Events;

public interface IFeatureReviewerRemoved : IEvent;

public sealed record FeatureReviewerRemovedV1(
    FeatureReviewerId ReviewerId,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeatureReviewerRemoved
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (FeatureStateData)stateData;
        var reviewerId = ReviewerId;
        var reviewer = state.Reviewers.Single(item => item.Id == reviewerId);
        state.Reviewers.Remove(reviewer);
        state.MemoryHistory.Add(new MemoryHistoryRecord(
            eventExecutionInfo.EventName, eventExecutionInfo.Timestamp, MemoryAggregateId));
        return state;
    }
}
