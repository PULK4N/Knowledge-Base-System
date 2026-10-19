using System.Text.Json;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using MemoryModule.Domain.Models;

namespace MemoryModule.Domain.Events;

public interface IClaudePromptUsageSet : IEvent;

/// <summary>Retained for deserialization; the original counts are ignored during replay.</summary>
public sealed record ClaudePromptUsageSetV1(
    PromptId PromptId,
    long InputTokens,
    long CacheReadInputTokens,
    long CacheCreationInputTokens,
    long OutputTokens,
    long? ThinkingTokens = null
) : IClaudePromptUsageSet
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo) => stateData;
}

/// <summary>Records prompt usage and the original JSON supplied by the migration or hook.</summary>
public sealed record ClaudePromptUsageSetV2(
    PromptId PromptId,
    long InputTokens,
    long CacheReadInputTokens,
    long CacheCreationInputTokens,
    long OutputTokens,
    JsonElement Payload,
    long? ThinkingTokens = null
) : IClaudePromptUsageSet
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (MemoryStateData)stateData;
        var prompt = state.ChatPrompts[PromptId];
        prompt.UsagePayload = Payload.Clone();
        prompt.InputTokens = checked(
            InputTokens + CacheReadInputTokens + CacheCreationInputTokens
        );
        prompt.CachedInputTokens = CacheReadInputTokens;
        prompt.CacheWriteInputTokens = CacheCreationInputTokens;
        prompt.OutputTokens = OutputTokens;
        prompt.ReasoningOutputTokens = ThinkingTokens;
        return state;
    }
}
