using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;
using ProjectModule.Shared;

namespace FeatureModule.Domain.Events;

public interface IFeaturePlanReviewerAdded : IEvent;

/// <summary>Adds a reviewer to a plan; it runs with the feature reviewers and is copied to the feature when the plan completes.</summary>
public sealed record FeaturePlanReviewerAddedV1(
    FeaturePlanId PlanId,
    FeatureReviewerId ReviewerId,
    string Title,
    string Description,
    ReviewLevel ReviewLevel,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeaturePlanReviewerAdded
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (FeatureStateData)stateData;
        var planId = PlanId;
        var plan = state.Plans.Single(item => item.Id == planId);
        plan.Reviewers.Add(new FeatureReviewer
        {
            Id = ReviewerId,
            Title = Title,
            Description = Description,
            ReviewLevel = ReviewLevel,
            CreatedAt = eventExecutionInfo.Timestamp,
            UpdatedAt = eventExecutionInfo.Timestamp
        });
        SpecificationHistory.Record(state, eventExecutionInfo, MemoryAggregateId);
        return state;
    }
}
