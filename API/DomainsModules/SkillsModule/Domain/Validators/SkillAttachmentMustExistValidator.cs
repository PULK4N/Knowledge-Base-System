using EventSourcing.Shared.Models;
using Shared.Interfaces;
using SkillsModule.Domain.Events;

namespace SkillsModule.Domain.Validators;

public sealed class SkillAttachmentMustExistValidator
    : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        if (payload.EventData is not SkillAttachmentDeletedV1 eventData)
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(SkillAttachmentMustExistValidator),
                false,
                $"{nameof(SkillAttachmentMustExistValidator)} can only validate "
                    + $"{nameof(SkillAttachmentDeletedV1)} events."
            );
        }

        var state = (SkillStateData)stateData;
        var attachmentExists = state.Attachments.ContainsKey(
            eventData.AttachmentId
        );

        return EventValidationResult.FromPayload(
            payload,
            nameof(SkillAttachmentMustExistValidator),
            attachmentExists,
            attachmentExists
                ? null
                : $"A skill attachment with ID '{eventData.AttachmentId}' does not exist."
        );
    }
}
