using EventSourcing.Shared.Models;
using MemoryModule.Domain.Events;
using Shared.Interfaces;

namespace MemoryModule.Domain.Validators;

/// <summary>
/// Rejects a model for a prompt that already has one, so the first model
/// recorded for a prompt is never replaced. A missing prompt is left to
/// <see cref="MemoryPromptMustExistValidator"/>.
/// </summary>
public sealed class PromptModelMustNotBeSetValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        if (payload.EventData is not PromptModelSetV1 eventData)
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(PromptModelMustNotBeSetValidator),
                false,
                $"{nameof(PromptModelMustNotBeSetValidator)} can only validate "
                    + $"{nameof(IPromptModelSet)} events."
            );
        }

        var state = (MemoryStateData)stateData;
        var modelIsSet =
            state.ChatPrompts.TryGetValue(eventData.PromptId, out var prompt)
            && !string.IsNullOrWhiteSpace(prompt.Model);

        return EventValidationResult.FromPayload(
            payload,
            nameof(PromptModelMustNotBeSetValidator),
            !modelIsSet,
            modelIsSet
                ? $"The prompt with turn ID '{eventData.PromptId.Value}' "
                    + $"already has the model '{prompt!.Model}'."
                : null
        );
    }
}
