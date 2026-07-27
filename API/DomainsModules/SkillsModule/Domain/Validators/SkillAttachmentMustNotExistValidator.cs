using EventSourcing.Shared.Models;
using Shared.Interfaces;
using SkillsModule.Domain.Events;

namespace SkillsModule.Domain.Validators;

public sealed class SkillAttachmentMustNotExistValidator
    : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        if (payload.EventData is not ISkillAttachmentAdded eventData)
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(SkillAttachmentMustNotExistValidator),
                false,
                $"{nameof(SkillAttachmentMustNotExistValidator)} can only validate "
                    + $"{nameof(ISkillAttachmentAdded)} events."
            );
        }

        var state = (SkillStateData)stateData;
        var attachmentExists = state.Attachments.ContainsKey(
            eventData.Attachment.Id
        );

        return EventValidationResult.FromPayload(
            payload,
            nameof(SkillAttachmentMustNotExistValidator),
            !attachmentExists,
            attachmentExists
                ? $"A skill attachment with ID '{eventData.Attachment.Id}' already exists."
                : null
        );
    }
}
