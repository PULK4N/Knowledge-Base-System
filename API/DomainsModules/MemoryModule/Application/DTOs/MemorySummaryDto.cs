using MemoryModule.Persistence.Interfaces;

namespace MemoryModule.Application.DTOs;

public sealed record MemorySummaryDto(
    Guid MemoryId,
    Guid ThreadId,
    string Summary,
    int PromptCount,
    DateTime? FirstPromptTimestamp,
    DateTime? LastPromptTimestamp,
    DateTime? SummaryTimestamp,
    DateTime LastActivityTimestamp
)
{
    public static MemorySummaryDto FromReadModel(
        MemorySummary memory
    ) =>
        new(
            memory.MemoryAggregateId.Value,
            memory.ThreadId.Value,
            memory.Summary,
            memory.PromptCount,
            memory.FirstPromptTimestamp,
            memory.LastPromptTimestamp,
            memory.SummaryTimestamp,
            memory.LastActivityTimestamp
        );
}
