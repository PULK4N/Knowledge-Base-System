using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeaturePlanReviewNoteRemoved : IEvent;

/// <summary>Removes a plan review note.</summary>
public sealed record FeaturePlanReviewNoteRemovedV1(
    FeaturePlanId PlanId,
    FeatureReviewNoteId ReviewNoteId,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeaturePlanReviewNoteRemoved
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (FeatureStateData)stateData;
        var planId = PlanId;
        var plan = state.Plans.Single(item => item.Id == planId);
        var reviewNoteId = ReviewNoteId;
        plan.ReviewNotes.RemoveAll(item => item.Id == reviewNoteId);
        SpecificationHistory.Record(state, eventExecutionInfo, MemoryAggregateId);
        return state;
    }
}
