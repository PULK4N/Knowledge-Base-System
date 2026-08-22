using EventSourcing.Shared.Models;
using Shared.Interfaces;

namespace FeatureModule.Domain.Validators;

public sealed class FeatureMustBeActiveValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var state = (FeatureStateData)stateData;

        return EventValidationResult.FromPayload(
            payload,
            nameof(FeatureMustBeActiveValidator),
            !state.IsDeleted,
            state.IsDeleted ? "The feature has been deleted." : null
        );
    }
}
