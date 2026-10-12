using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using Shared.Interfaces;

namespace FeatureModule.Domain.Validators;

// Validators stay registered for released plan events that are kept for replay.
#pragma warning disable CS0618

public sealed class FeaturePlanMustNotExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var planId = payload.EventData switch
        {
            FeaturePlanAddedV1 eventData => eventData.PlanId,
            FeaturePlanAddedV2 eventData => eventData.PlanId,
            FeaturePlanAddedV3 eventData => eventData.PlanId,
            _ => (FeaturePlanId?)null
        };

        if (planId is null)
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(FeaturePlanMustNotExistValidator),
                false,
                $"{nameof(FeaturePlanMustNotExistValidator)} can only validate plan addition events."
            );
        }

        var state = (FeatureStateData)stateData;
        var exists = state.Plans.Any(plan => plan.Id == planId.Value);

        return EventValidationResult.FromPayload(
            payload,
            nameof(FeaturePlanMustNotExistValidator),
            !exists,
            exists ? "The feature plan already exists." : null
        );
    }
}
