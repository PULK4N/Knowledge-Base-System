using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeaturePlanReviewerRemoved : IEvent;

/// <summary>Removes a plan reviewer.</summary>
public sealed record FeaturePlanReviewerRemovedV1(
    FeaturePlanId PlanId,
    FeatureReviewerId ReviewerId,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeaturePlanReviewerRemoved
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (FeatureStateData)stateData;
        var planId = PlanId;
        var plan = state.Plans.Single(item => item.Id == planId);
        var reviewerId = ReviewerId;
        plan.Reviewers.RemoveAll(item => item.Id == reviewerId);
        SpecificationHistory.Record(state, eventExecutionInfo, MemoryAggregateId);
        return state;
    }
}
