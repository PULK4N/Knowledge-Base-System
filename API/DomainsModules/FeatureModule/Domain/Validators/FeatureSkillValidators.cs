using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using Shared.Interfaces;

namespace FeatureModule.Domain.Validators;

public sealed class FeatureSkillMustNotExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        if (payload.EventData is not FeatureSkillAddedV1 eventData)
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(FeatureSkillMustNotExistValidator),
                false,
                $"{nameof(FeatureSkillMustNotExistValidator)} can only validate {nameof(FeatureSkillAddedV1)} events."
            );
        }

        var state = (FeatureStateData)stateData;
        var exists = state.RelatedSkillIds.Contains(eventData.SkillId);

        return EventValidationResult.FromPayload(
            payload,
            nameof(FeatureSkillMustNotExistValidator),
            !exists,
            exists ? "The skill is already related to the feature." : null
        );
    }
}

public sealed class FeatureSkillMustExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        if (payload.EventData is not FeatureSkillRemovedV1 eventData)
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(FeatureSkillMustExistValidator),
                false,
                $"{nameof(FeatureSkillMustExistValidator)} can only validate {nameof(FeatureSkillRemovedV1)} events."
            );
        }

        var state = (FeatureStateData)stateData;
        var exists = state.RelatedSkillIds.Contains(eventData.SkillId);

        return EventValidationResult.FromPayload(
            payload,
            nameof(FeatureSkillMustExistValidator),
            exists,
            exists ? null : "The skill is not related to the feature."
        );
    }
}
