using System.Text.Json;
using EventSourcing.Shared.Models;
using MemoryModule.Domain.Events;
using Shared.Interfaces;

namespace MemoryModule.Domain.Validators;

public sealed class PromptUsageMustBeValidValidator : IPreEventValidator
{
    private static bool HasSourceRecords(JsonElement payload) =>
        payload.ValueKind == JsonValueKind.Object
        && payload.TryGetProperty("sourceRecords", out var records)
        && records.ValueKind == JsonValueKind.Array
        && records.GetArrayLength() > 0
        && records.EnumerateArray().All(record => record.ValueKind == JsonValueKind.Object);

    public EventValidationResult Validate(object stateData, EventPayload payload)
    {
        var valid = payload.EventData switch
        {
            ClaudePromptUsageSetV2 usage =>
                HasSourceRecords(usage.Payload)
                && usage.InputTokens >= 0
                && usage.CacheReadInputTokens >= 0
                && usage.CacheCreationInputTokens >= 0
                && (decimal)usage.InputTokens + usage.CacheReadInputTokens
                    + usage.CacheCreationInputTokens + usage.OutputTokens <= long.MaxValue
                && usage.OutputTokens >= 0
                && (usage.ThinkingTokens is null
                    || usage.ThinkingTokens >= 0 && usage.ThinkingTokens <= usage.OutputTokens),
            CodexPromptUsageSetV2 usage =>
                HasSourceRecords(usage.Payload)
                && usage.InputTokens >= 0
                && usage.CachedInputTokens >= 0
                && usage.CacheWriteInputTokens >= 0
                && (decimal)usage.CachedInputTokens + usage.CacheWriteInputTokens <= usage.InputTokens
                && usage.OutputTokens >= 0
                && (decimal)usage.InputTokens + usage.OutputTokens <= long.MaxValue
                && (usage.ReasoningOutputTokens is null
                    || usage.ReasoningOutputTokens >= 0 && usage.ReasoningOutputTokens <= usage.OutputTokens),
            _ => false
        };

        return EventValidationResult.FromPayload(
            payload,
            nameof(PromptUsageMustBeValidValidator),
            valid,
            valid ? null : "Prompt usage must include source records and nonnegative counts with cache and reasoning counts within their totals."
        );
    }
}
