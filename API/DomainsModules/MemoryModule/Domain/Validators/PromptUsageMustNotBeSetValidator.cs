using EventSourcing.Shared.Models;
using MemoryModule.Domain.Events;
using MemoryModule.Domain.Models;
using Shared.Interfaces;

namespace MemoryModule.Domain.Validators;

/// <summary>
/// Preserves the first recorded usage, including partially populated historical
/// counts. A missing prompt is handled by MemoryPromptMustExistValidator.
/// </summary>
public sealed class PromptUsageMustNotBeSetValidator : IPreEventValidator
{
    public EventValidationResult Validate(object stateData, EventPayload payload)
    {
        PromptId? promptId = payload.EventData switch
        {
            ClaudePromptUsageSetV1 usage => usage.PromptId,
            CodexPromptUsageSetV1 usage => usage.PromptId,
            _ => null
        };
        if (promptId is not { } id)
            return EventValidationResult.FromPayload(
                payload, nameof(PromptUsageMustNotBeSetValidator), false,
                "This validator requires a Claude or Codex prompt usage event."
            );

        var state = (MemoryStateData)stateData;
        var alreadySet = state.ChatPrompts.TryGetValue(id, out var prompt)
            && (prompt.InputTokens.HasValue
                || prompt.CachedInputTokens.HasValue
                || prompt.CacheWriteInputTokens.HasValue
                || prompt.OutputTokens.HasValue
                || prompt.ReasoningOutputTokens.HasValue);
        return EventValidationResult.FromPayload(
            payload, nameof(PromptUsageMustNotBeSetValidator), !alreadySet,
            alreadySet ? $"Usage is already set for prompt '{id.Value}'." : null
        );
    }
}
