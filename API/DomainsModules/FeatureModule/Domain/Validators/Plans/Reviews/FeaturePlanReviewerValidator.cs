using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using ProjectModule.Shared;
using Shared.Interfaces;

namespace FeatureModule.Domain.Validators;

/// <summary>Plan reviewers belong to an existing plan and need a title, description and known review level.</summary>
public sealed class FeaturePlanReviewerValidator : IPreEventValidator
{
    public EventValidationResult Validate(object stateData, EventPayload payload)
    {
        var (planId, reviewerId, mustExist, title, description, level) = payload.EventData switch
        {
            FeaturePlanReviewerAddedV1 data => (data.PlanId, data.ReviewerId, false, data.Title, data.Description, data.ReviewLevel),
            FeaturePlanReviewerUpdatedV1 data => (data.PlanId, data.ReviewerId, true, data.Title, data.Description, data.ReviewLevel),
            FeaturePlanReviewerRemovedV1 data => (data.PlanId, data.ReviewerId, true, "-", "-", ReviewLevel.Medium),
            _ => (default(FeaturePlanId?), default(FeatureReviewerId), false, "", "", ReviewLevel.Medium)
        };
        var plan = ((FeatureStateData)stateData).Plans.SingleOrDefault(item => item.Id == planId);
        var error = planId is null
            ? $"{nameof(FeaturePlanReviewerValidator)} cannot validate {payload.EventData.GetType().Name}."
            : plan is null ? "The feature plan does not exist."
            : plan.Reviewers.Any(item => item.Id == reviewerId) != mustExist
                ? mustExist ? "The plan reviewer does not exist." : "The plan reviewer already exists."
            : string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description) || !Enum.IsDefined(level)
                ? "A title, description and valid review level are required."
                : null;
        return EventValidationResult.FromPayload(payload, nameof(FeaturePlanReviewerValidator), error is null, error);
    }
}
