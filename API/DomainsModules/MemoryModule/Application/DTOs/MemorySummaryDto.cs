using MemoryModule.Persistence.Interfaces;

namespace MemoryModule.Application.DTOs;

public sealed record MemorySummaryDto(
    Guid MemoryId,
    Guid ThreadId,
    string SessionTitle,
    string Summary,
    int PromptCount,
    DateTime? FirstPromptTimestamp,
    DateTime? LastPromptTimestamp,
    DateTime? SummaryTimestamp,
    DateTime LastActivityTimestamp,
    List<string> Models,
    long? InputTokens = null,
    long? CachedInputTokens = null,
    long? CacheWriteInputTokens = null,
    long? OutputTokens = null,
    long? ReasoningOutputTokens = null
)
{
    public long? TotalTokens => InputTokens + OutputTokens;

    public static MemorySummaryDto FromReadModel(
        MemorySummary memory
    ) =>
        new(
            memory.MemoryAggregateId.Value,
            memory.ThreadId.Value,
            memory.SessionTitle,
            memory.Summary,
            memory.PromptCount,
            memory.FirstPromptTimestamp,
            memory.LastPromptTimestamp,
            memory.SummaryTimestamp,
            memory.LastActivityTimestamp,
            memory.Models.ToList(),
            memory.InputTokens,
            memory.CachedInputTokens,
            memory.CacheWriteInputTokens,
            memory.OutputTokens,
            memory.ReasoningOutputTokens
        );
}
