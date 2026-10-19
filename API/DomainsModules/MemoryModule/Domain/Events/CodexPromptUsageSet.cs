using System.Text.Json;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using MemoryModule.Domain.Models;

namespace MemoryModule.Domain.Events;

public interface ICodexPromptUsageSet : IEvent;

/// <summary>Retained for deserialization; the original counts are ignored during replay.</summary>
public sealed record CodexPromptUsageSetV1(
    PromptId PromptId,
    long InputTokens,
    long CachedInputTokens,
    long CacheWriteInputTokens,
    long OutputTokens,
    long? ReasoningOutputTokens = null
) : ICodexPromptUsageSet
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo) => stateData;
}

/// <summary>Records prompt usage and the original JSON supplied by the migration or hook.</summary>
public sealed record CodexPromptUsageSetV2(
    PromptId PromptId,
    long InputTokens,
    long CachedInputTokens,
    long CacheWriteInputTokens,
    long OutputTokens,
    JsonElement Payload,
    long? ReasoningOutputTokens = null
) : ICodexPromptUsageSet
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (MemoryStateData)stateData;
        var prompt = state.ChatPrompts[PromptId];
        prompt.UsagePayload = Payload.Clone();
        prompt.InputTokens = InputTokens;
        prompt.CachedInputTokens = CachedInputTokens;
        prompt.CacheWriteInputTokens = CacheWriteInputTokens;
        prompt.OutputTokens = OutputTokens;
        prompt.ReasoningOutputTokens = ReasoningOutputTokens;
        return state;
    }
}
