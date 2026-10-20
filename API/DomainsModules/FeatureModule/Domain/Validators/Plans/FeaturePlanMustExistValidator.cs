using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using Shared.Interfaces;

namespace FeatureModule.Domain.Validators;

// Validators stay registered for released plan events that are kept for replay.
#pragma warning disable CS0618

public sealed class FeaturePlanMustExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var planId = payload.EventData switch
        {
            CurrentFeaturePlanChangedV1 eventData => eventData.PlanId,
            CurrentFeaturePlanChangedV2 eventData => eventData.PlanId,
            FeaturePlanRemovedV1 eventData => eventData.PlanId,
            FeaturePlanRemovedV2 eventData => eventData.PlanId,
            FeaturePlanUpdatedV1 eventData => eventData.PlanId,
            FeaturePlanStatusChangedV1 eventData => eventData.PlanId,
            FeaturePlanBranchAddedV1 eventData => eventData.PlanId,
            FeaturePlanBranchUpdatedV1 eventData => eventData.PlanId,
            FeaturePlanBranchRemovedV1 eventData => eventData.PlanId,
            FeaturePlanPreviousPlanSetV1 eventData => eventData.PlanId,
            FeaturePlanRecordsSetV1 eventData => eventData.PlanId,
            _ => (FeaturePlanId?)null
        };

        if (planId is null)
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(FeaturePlanMustExistValidator),
                false,
                $"{nameof(FeaturePlanMustExistValidator)} can only validate plan or plan branch events for an existing plan."
            );
        }

        var state = (FeatureStateData)stateData;
        var exists = state.Plans.Any(plan => plan.Id == planId.Value);

        return EventValidationResult.FromPayload(
            payload,
            nameof(FeaturePlanMustExistValidator),
            exists,
            exists ? null : "The feature plan does not exist."
        );
    }
}
