using EventSourcing.Shared.Models;
using Shared.Interfaces;
using SkillsModule.Domain.Events;

namespace SkillsModule.Domain.Validators;

public sealed class SkillReferenceMustExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(object stateData, EventPayload payload)
    {
        var relativePath = payload.EventData switch
        {
            SkillReferenceUpdatedV1 eventData => eventData.RelativePath,
            SkillReferenceUpdatedV2 eventData => eventData.RelativePath,
            SkillReferenceDeletedV1 eventData => eventData.RelativePath,
            _ => null
        };

        if (relativePath is null)
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(SkillReferenceMustExistValidator),
                false,
                $"{nameof(SkillReferenceMustExistValidator)} can only validate "
                    + $"{nameof(ISkillReferenceUpdated)} or "
                    + $"{nameof(SkillReferenceDeletedV1)} events."
            );
        }

        var state = (SkillStateData)stateData;
        var referenceExists = state.References.ContainsKey(relativePath);

        return EventValidationResult.FromPayload(
            payload,
            nameof(SkillReferenceMustExistValidator),
            referenceExists,
            referenceExists
                ? null
                : $"A skill reference with relative path '{relativePath}' does not exist."
        );
    }
}
