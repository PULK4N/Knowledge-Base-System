using EventSourcing.Shared.Models;
using SettingsModule.Domain.Events;
using Shared.Interfaces;

namespace SettingsModule.Domain.Validators;

public sealed class CodeReviewSettingsValidValidator : IPreEventValidator
{
    public EventValidationResult Validate(object stateData, EventPayload payload)
    {
        var (batchSize, taskMessage) = payload.EventData switch
        {
            SettingsInitializedV1 value => (value.BatchSize, value.TaskMessage),
            CodeReviewSettingsUpdatedV1 value => (value.BatchSize, value.TaskMessage),
            _ => throw new InvalidCastException()
        };
        var reason = batchSize <= 0 ? "Reviewer batch size must be positive."
            : string.IsNullOrWhiteSpace(taskMessage) ? "Review task message is required." : null;
        return EventValidationResult.FromPayload(payload, nameof(CodeReviewSettingsValidValidator),
            reason is null, reason);
    }
}
