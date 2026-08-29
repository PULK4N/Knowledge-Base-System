using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using Shared.Interfaces;

namespace FeatureModule.Domain.Validators;

public sealed class FeatureCannotParentItselfValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        if (payload.EventData is not FeatureParentSetV1 eventData)
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(FeatureCannotParentItselfValidator),
                false,
                $"{nameof(FeatureCannotParentItselfValidator)} can only validate {nameof(FeatureParentSetV1)} events."
            );
        }

        var state = (FeatureStateData)stateData;
        var isSelfParent = eventData.ParentFeatureId == state.Id;

        return EventValidationResult.FromPayload(
            payload,
            nameof(FeatureCannotParentItselfValidator),
            !isSelfParent,
            isSelfParent ? "A feature cannot be its own parent." : null
        );
    }
}
