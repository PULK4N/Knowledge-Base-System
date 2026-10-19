using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using MemoryModule.Domain.Models;

namespace MemoryModule.Domain.Events;

public interface IClaudePromptUsageSet : IEvent;

/// <summary>
/// Sets the complete usage for one prompt. YAML validation rejects new events
/// once usage is recorded; historical replay applies the persisted values.
/// Input counts follow Claude's native usage format.
/// </summary>
public sealed record ClaudePromptUsageSetV1(
    PromptId PromptId,
    long InputTokens,
    long CacheReadInputTokens,
    long CacheCreationInputTokens,
    long OutputTokens,
    long? ThinkingTokens = null
) : IClaudePromptUsageSet
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (MemoryStateData)stateData;
        var prompt = state.ChatPrompts[PromptId];
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
