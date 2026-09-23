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
        var skillId = payload.EventData switch
        {
            FeatureSkillAddedV1 eventData => eventData.SkillId,
            FeatureSkillAddedV2 eventData => eventData.SkillId,
            _ => (AggregateId?)null
        };

        if (skillId is null)
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(FeatureSkillMustNotExistValidator),
                false,
                $"{nameof(FeatureSkillMustNotExistValidator)} can only validate {nameof(FeatureSkillAddedV1)} or {nameof(FeatureSkillAddedV2)} events."
            );
        }

        var state = (FeatureStateData)stateData;
        var exists = state.RelatedSkillIds.Contains(skillId.Value);

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
        var skillId = payload.EventData switch
        {
            FeatureSkillRemovedV1 eventData => eventData.SkillId,
            FeatureSkillRemovedV2 eventData => eventData.SkillId,
            _ => (AggregateId?)null
        };

        if (skillId is null)
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(FeatureSkillMustExistValidator),
                false,
                $"{nameof(FeatureSkillMustExistValidator)} can only validate {nameof(FeatureSkillRemovedV1)} or {nameof(FeatureSkillRemovedV2)} events."
            );
        }

        var state = (FeatureStateData)stateData;
        var exists = state.RelatedSkillIds.Contains(skillId.Value);

        return EventValidationResult.FromPayload(
            payload,
            nameof(FeatureSkillMustExistValidator),
            exists,
            exists ? null : "The skill is not related to the feature."
        );
    }
}
