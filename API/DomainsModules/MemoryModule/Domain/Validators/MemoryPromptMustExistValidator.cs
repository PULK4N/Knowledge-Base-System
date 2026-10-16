using EventSourcing.Shared.Models;
using MemoryModule.Domain.Events;
using Shared.Interfaces;

namespace MemoryModule.Domain.Validators;

public sealed class MemoryPromptMustExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        if (payload.EventData is not MemoryAttachmentAddedV1 eventData)
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(MemoryPromptMustExistValidator),
                false,
                $"{nameof(MemoryPromptMustExistValidator)} can only validate "
                    + $"{nameof(IMemoryAttachmentAdded)} events."
            );
        }

        var state = (MemoryStateData)stateData;
        var promptExists = state.ChatPrompts.ContainsKey(eventData.PromptId);

        return EventValidationResult.FromPayload(
            payload,
            nameof(MemoryPromptMustExistValidator),
            promptExists,
            promptExists
                ? null
                : $"The memory has no prompt with turn ID "
                    + $"'{eventData.PromptId.Value}'."
        );
    }
}
