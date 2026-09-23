using EventSourcing.Shared.Models;
using Shared.Interfaces;
using SkillsModule.Domain.Events;
using SkillsModule.Domain.Models;

namespace SkillsModule.Domain.Validators;

public sealed class SkillAttachmentMustExistValidator
    : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        FileId? attachmentId = payload.EventData switch
        {
            SkillAttachmentDeletedV1 eventData => eventData.AttachmentId,
            SkillAttachmentDeletedV2 eventData => eventData.AttachmentId,
            _ => null
        };
        if (attachmentId is null)
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(SkillAttachmentMustExistValidator),
                false,
                $"{nameof(SkillAttachmentMustExistValidator)} can only validate "
                    + $"{nameof(ISkillAttachmentDeleted)} events."
            );
        }

        var state = (SkillStateData)stateData;
        var attachmentExists = state.Attachments.ContainsKey(
            attachmentId.Value
        );

        return EventValidationResult.FromPayload(
            payload,
            nameof(SkillAttachmentMustExistValidator),
            attachmentExists,
            attachmentExists
                ? null
                : $"A skill attachment with ID '{attachmentId.Value}' does not exist."
        );
    }
}
