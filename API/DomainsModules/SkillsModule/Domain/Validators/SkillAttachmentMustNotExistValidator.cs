using EventSourcing.Shared.Models;
using Shared.Interfaces;
using SkillsModule.Domain.Events;
using SkillsModule.Domain.Models;

namespace SkillsModule.Domain.Validators;

public sealed class SkillAttachmentMustNotExistValidator
    : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        FileId? attachmentId = payload.EventData switch
        {
            SkillAttachmentAddedV1 eventData => eventData.Attachment.Id,
            SkillAttachmentAddedV2 eventData => eventData.Attachment.Id,
            _ => null
        };
        if (attachmentId is null)
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
            attachmentId.Value
        );

        return EventValidationResult.FromPayload(
            payload,
            nameof(SkillAttachmentMustNotExistValidator),
            !attachmentExists,
            attachmentExists
                ? $"A skill attachment with ID '{attachmentId.Value}' already exists."
                : null
        );
    }
}
