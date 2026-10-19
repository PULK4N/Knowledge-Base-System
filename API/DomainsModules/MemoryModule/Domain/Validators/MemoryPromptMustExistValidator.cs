using EventSourcing.Shared.Models;
using MemoryModule.Domain.Events;
using MemoryModule.Domain.Models;
using Shared.Interfaces;

namespace MemoryModule.Domain.Validators;

public sealed class MemoryPromptMustExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        PromptId? promptId = payload.EventData switch
        {
            MemoryAttachmentAddedV1 eventData => eventData.PromptId,
            PromptModelSetV1 eventData => eventData.PromptId,
            ClaudePromptUsageSetV1 eventData => eventData.PromptId,
            CodexPromptUsageSetV1 eventData => eventData.PromptId,
            _ => null
        };

        if (promptId is not { } targetPromptId)
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(MemoryPromptMustExistValidator),
                false,
                $"{nameof(MemoryPromptMustExistValidator)} can only validate "
                    + $"{nameof(IMemoryAttachmentAdded)} and "
                    + $"{nameof(IPromptModelSet)}, {nameof(IClaudePromptUsageSet)}, "
                    + $"and {nameof(ICodexPromptUsageSet)} events."
            );
        }

        var state = (MemoryStateData)stateData;
        var promptExists = state.ChatPrompts.ContainsKey(targetPromptId);

        return EventValidationResult.FromPayload(
            payload,
            nameof(MemoryPromptMustExistValidator),
            promptExists,
            promptExists
                ? null
                : $"The memory has no prompt with turn ID "
                    + $"'{targetPromptId.Value}'."
        );
    }
}
