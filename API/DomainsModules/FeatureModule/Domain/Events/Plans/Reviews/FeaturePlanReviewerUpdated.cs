using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;
using ProjectModule.Shared;

namespace FeatureModule.Domain.Events;

public interface IFeaturePlanReviewerUpdated : IEvent;

/// <summary>Replaces the title, description and review level of a plan reviewer.</summary>
public sealed record FeaturePlanReviewerUpdatedV1(
    FeaturePlanId PlanId,
    FeatureReviewerId ReviewerId,
    string Title,
    string Description,
    ReviewLevel ReviewLevel,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeaturePlanReviewerUpdated
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (FeatureStateData)stateData;
        var planId = PlanId;
        var plan = state.Plans.Single(item => item.Id == planId);
        var reviewerId = ReviewerId;
        var reviewer = plan.Reviewers.Single(item => item.Id == reviewerId);
        reviewer.Title = Title;
        reviewer.Description = Description;
        reviewer.ReviewLevel = ReviewLevel;
        reviewer.UpdatedAt = eventExecutionInfo.Timestamp;
        SpecificationHistory.Record(state, eventExecutionInfo, MemoryAggregateId);
        return state;
    }
}
