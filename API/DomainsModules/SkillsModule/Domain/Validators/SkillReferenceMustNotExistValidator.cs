using EventSourcing.Shared.Models;
using Shared.Interfaces;
using SkillsModule.Domain.Events;

namespace SkillsModule.Domain.Validators;

public sealed class SkillReferenceMustNotExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(object stateData, EventPayload payload)
    {
        var relativePath = payload.EventData switch
        {
            SkillReferenceAddedV1 eventData => eventData.RelativePath,
            SkillReferenceAddedV2 eventData => eventData.RelativePath,
            _ => null
        };

        if (relativePath is null)
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(SkillReferenceMustNotExistValidator),
                false,
                $"{nameof(SkillReferenceMustNotExistValidator)} can only validate "
                    + $"{nameof(ISkillReferenceAdded)} events."
            );
        }

        var state = (SkillStateData)stateData;
        var referenceExists = state.References.ContainsKey(
            relativePath
        );

        return EventValidationResult.FromPayload(
            payload,
            nameof(SkillReferenceMustNotExistValidator),
            !referenceExists,
            referenceExists
                ? $"A skill reference with relative path '{relativePath}' already exists."
                : null
        );
    }
}
