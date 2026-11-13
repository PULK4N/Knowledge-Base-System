using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using Shared.Interfaces;

namespace FeatureModule.Domain.Validators;

/// <summary>Plan review notes belong to an existing plan and need a title and content.</summary>
public sealed class FeaturePlanReviewNoteValidator : IPreEventValidator
{
    public EventValidationResult Validate(object stateData, EventPayload payload)
    {
        var (planId, reviewNoteId, mustExist, title, content) = payload.EventData switch
        {
            FeaturePlanReviewNoteAddedV1 data => (data.PlanId, data.ReviewNoteId, false, data.Title, data.Content),
            FeaturePlanReviewNoteUpdatedV1 data => (data.PlanId, data.ReviewNoteId, true, data.Title, data.Content),
            FeaturePlanReviewNoteRemovedV1 data => (data.PlanId, data.ReviewNoteId, true, "-", "-"),
            _ => (default(FeaturePlanId?), default(FeatureReviewNoteId), false, "", "")
        };
        var plan = ((FeatureStateData)stateData).Plans.SingleOrDefault(item => item.Id == planId);
        var error = planId is null
            ? $"{nameof(FeaturePlanReviewNoteValidator)} cannot validate {payload.EventData.GetType().Name}."
            : plan is null ? "The feature plan does not exist."
            : plan.ReviewNotes.Any(item => item.Id == reviewNoteId) != mustExist
                ? mustExist ? "The plan review note does not exist." : "The plan review note already exists."
            : string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content)
                ? "A review note title and content are required."
                : null;
        return EventValidationResult.FromPayload(payload, nameof(FeaturePlanReviewNoteValidator), error is null, error);
    }
}
