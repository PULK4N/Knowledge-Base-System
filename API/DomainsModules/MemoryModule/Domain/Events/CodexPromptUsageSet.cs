using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using MemoryModule.Domain.Models;

namespace MemoryModule.Domain.Events;

public interface ICodexPromptUsageSet : IEvent;

/// <summary>
/// Sets the complete usage for one prompt, replacing any earlier counts.
/// Input counts follow Codex's native usage format.
/// </summary>
public sealed record CodexPromptUsageSetV1(
    PromptId PromptId,
    long InputTokens,
    long CachedInputTokens,
    long CacheWriteInputTokens,
    long OutputTokens,
    long? ReasoningOutputTokens = null
) : ICodexPromptUsageSet
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (MemoryStateData)stateData;
        var prompt = state.ChatPrompts[PromptId];
        prompt.InputTokens = InputTokens;
        prompt.CachedInputTokens = CachedInputTokens;
        prompt.CacheWriteInputTokens = CacheWriteInputTokens;
        prompt.OutputTokens = OutputTokens;
        prompt.ReasoningOutputTokens = ReasoningOutputTokens;
        return state;
    }
}
