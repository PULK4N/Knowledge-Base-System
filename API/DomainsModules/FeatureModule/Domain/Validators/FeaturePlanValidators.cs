using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using Shared.Interfaces;

namespace FeatureModule.Domain.Validators;

public sealed class FeaturePlanMustNotExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        if (payload.EventData is not FeaturePlanAddedV1 eventData)
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(FeaturePlanMustNotExistValidator),
                false,
                $"{nameof(FeaturePlanMustNotExistValidator)} can only validate {nameof(FeaturePlanAddedV1)} events."
            );
        }

        var state = (FeatureStateData)stateData;
        var exists = state.Plans.Any(plan => plan.Id == eventData.PlanId);

        return EventValidationResult.FromPayload(
            payload,
            nameof(FeaturePlanMustNotExistValidator),
            !exists,
            exists ? "The feature plan already exists." : null
        );
    }
}

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
            FeaturePlanRemovedV1 eventData => eventData.PlanId,
            _ => (FeaturePlanId?)null
        };

        if (planId is null)
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(FeaturePlanMustExistValidator),
                false,
                $"{nameof(FeaturePlanMustExistValidator)} can only validate plan selection or removal events."
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

public sealed class CurrentFeaturePlanMustExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        if (payload.EventData is not CurrentFeaturePlanUpdatedV1)
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(CurrentFeaturePlanMustExistValidator),
                false,
                $"{nameof(CurrentFeaturePlanMustExistValidator)} can only validate {nameof(CurrentFeaturePlanUpdatedV1)} events."
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
