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
/// Filters allow callers to narrow tool-call content by tool name.
/// </summary>
public sealed record MemoryToolCallFilters(string? ToolName = null);

public enum MemoryToolCallSortField
{
    Timestamp,
    ToolName
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
