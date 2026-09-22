using EventSourcing.Shared.Models;
using MemoryModule.Domain.Models;

namespace MemoryModule.Persistence.Interfaces;

/// <summary>
/// One recorded tool invocation of a prompt. It is stored as plain rows with
/// the raw payload preserved, because tool calls are inspected and filtered by
/// tool name rather than retrieved by similarity.
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

public interface IMemoryToolCallRepository
{
    Task<List<MemoryToolCall>> Get(
        AggregateId memoryAggregateId,
        CancellationToken cancellationToken = default
    );

    Task Write(
        IReadOnlyCollection<AggregateId> memoryAggregateIds,
        IReadOnlyCollection<MemoryToolCall> toolCalls,
        CancellationToken cancellationToken = default
    );
}
