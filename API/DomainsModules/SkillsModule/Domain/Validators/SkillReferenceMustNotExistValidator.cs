using EventSourcing.Shared.Models;
using Shared.Interfaces;
using SkillsModule.Domain.Events;

namespace SkillsModule.Domain.Validators;

public sealed class SkillReferenceMustNotExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(object stateData, EventPayload payload)
    {
        if (payload.EventData is not SkillReferenceAdded eventData)
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(SkillReferenceMustNotExistValidator),
                false,
                $"{nameof(SkillReferenceMustNotExistValidator)} can only validate "
                    + $"{nameof(SkillReferenceAdded)} events."
            );
        }

        var state = (SkillStateData)stateData;
        var referenceExists = state.References.ContainsKey(
            eventData.RelativePath
        );

        return EventValidationResult.FromPayload(
            payload,
            nameof(SkillReferenceMustNotExistValidator),
            !referenceExists,
            referenceExists
                ? $"A skill reference with relative path '{eventData.RelativePath}' already exists."
                : null
        );
    }
}
