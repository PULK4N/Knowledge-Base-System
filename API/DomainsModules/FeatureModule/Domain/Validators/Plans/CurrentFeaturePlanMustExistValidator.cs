using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using Shared.Interfaces;

namespace FeatureModule.Domain.Validators;

// Validators stay registered for released plan events that are kept for replay.
#pragma warning disable CS0618

[Obsolete("Kept for the YAML definition of released current-plan events.")]
public sealed class CurrentFeaturePlanMustExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        if (payload.EventData is not (CurrentFeaturePlanUpdatedV1 or CurrentFeaturePlanUpdatedV2))
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(CurrentFeaturePlanMustExistValidator),
                false,
                $"{nameof(CurrentFeaturePlanMustExistValidator)} can only validate {nameof(CurrentFeaturePlanUpdatedV1)} or {nameof(CurrentFeaturePlanUpdatedV2)} events."
            );
        }

        var state = (FeatureStateData)stateData;
        var exists = state.CurrentPlanId is { } currentPlanId
            && state.Plans.Any(plan => plan.Id == currentPlanId);

        return EventValidationResult.FromPayload(
            payload,
            nameof(CurrentFeaturePlanMustExistValidator),
            exists,
            exists ? null : "The feature does not have a current plan."
        );
    }
}
