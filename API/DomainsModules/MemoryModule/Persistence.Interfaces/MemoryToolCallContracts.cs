using ActionModule.Shared.Models;
using EventSourcing.Shared.Models;
using MemoryModule.Domain.Models;

namespace MemoryModule.Persistence.Interfaces;

/// <summary>
/// One recorded tool invocation of a prompt. It is stored as plain rows with
/// the raw payload preserved so callers can inspect it and search within its
/// content directly.
/// </summary>
public sealed record MemoryToolCall(
    AggregateId MemoryAggregateId,
    ThreadId ThreadId,
    PromptId PromptId,
    int ToolCallIndex,
    DateTime Timestamp,
    string ToolName,
    string ToolUseId,
    string Description,
    string PayloadJson
);

/// <summary>
/// Filters narrow tool calls by tool name, by the chat (memory or thread) and
/// prompt they belong to, and by full-text keywords. Input keywords must all
/// appear in the tool input and output keywords must all appear in the tool
/// response; an empty list leaves that side unfiltered.
/// </summary>
public sealed record MemoryToolCallFilters(
    string? ToolName = null,
    AggregateId? MemoryAggregateId = null,
    ThreadId? ThreadId = null,
    PromptId? PromptId = null,
    List<string>? InputKeywords = null,
    List<string>? OutputKeywords = null
);

public enum MemoryToolCallSortField
{
    Timestamp,
    ToolName,
    /// <summary>
    /// Full-text rank of the input and output keywords. Without keywords it
    /// falls back to timestamp order.
    /// </summary>
    Relevance
}

public interface IMemoryToolCallRepository
{
    Task<List<MemoryToolCall>> Get(
        AggregateId memoryAggregateId,
        CancellationToken cancellationToken = default
    );

    Task<PagedResult<MemoryToolCall>> Search(
        EntityQuery<MemoryToolCallFilters, MemoryToolCallSortField> request,
        CancellationToken cancellationToken = default
    );

    Task Write(
        IReadOnlyCollection<AggregateId> memoryAggregateIds,
        IReadOnlyCollection<MemoryToolCall> toolCalls,
        CancellationToken cancellationToken = default
    );
}
