using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeaturePlanReviewNoteUpdated : IEvent;

/// <summary>Replaces the title and content of a plan review note.</summary>
public sealed record FeaturePlanReviewNoteUpdatedV1(
    FeaturePlanId PlanId,
    FeatureReviewNoteId ReviewNoteId,
    string Title,
    string Content,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeaturePlanReviewNoteUpdated
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (FeatureStateData)stateData;
        var planId = PlanId;
        var plan = state.Plans.Single(item => item.Id == planId);
        var reviewNoteId = ReviewNoteId;
        var reviewNote = plan.ReviewNotes.Single(item => item.Id == reviewNoteId);
        reviewNote.Title = Title;
        reviewNote.Content = Content;
        reviewNote.UpdatedAt = eventExecutionInfo.Timestamp;
        SpecificationHistory.Record(state, eventExecutionInfo, MemoryAggregateId);
        return state;
    }
}
