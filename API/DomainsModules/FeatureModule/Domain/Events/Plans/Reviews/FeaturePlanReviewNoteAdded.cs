using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeaturePlanReviewNoteAdded : IEvent;

/// <summary>Adds a review note to a plan; it is copied to the feature when the plan completes.</summary>
public sealed record FeaturePlanReviewNoteAddedV1(
    FeaturePlanId PlanId,
    FeatureReviewNoteId ReviewNoteId,
    string Title,
    string Content,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeaturePlanReviewNoteAdded
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (FeatureStateData)stateData;
        var planId = PlanId;
        var plan = state.Plans.Single(item => item.Id == planId);
        plan.ReviewNotes.Add(new FeatureReviewNote
        {
            Id = ReviewNoteId,
            Title = Title,
            Content = Content,
            CreatedAt = eventExecutionInfo.Timestamp,
            UpdatedAt = eventExecutionInfo.Timestamp
        });
        SpecificationHistory.Record(state, eventExecutionInfo, MemoryAggregateId);
        return state;
    }
}
