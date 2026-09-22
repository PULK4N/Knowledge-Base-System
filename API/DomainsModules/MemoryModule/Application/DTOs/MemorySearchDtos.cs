using MemoryModule.Persistence.Interfaces;

namespace MemoryModule.Application.DTOs;

public sealed record MemorySearchQueryResult(
    string Message,
    List<MemorySearchMatchDto> Results,
    int ApproximateTokenCount,
    bool IsTruncated
);

public sealed record MemorySearchMatchDto(
    Guid MemoryId,
    Guid ThreadId,
    Guid? PromptId,
    DateTime MatchedAt,
    bool MatchedSummary,
    string Summary,
    string? MatchedText
);

public sealed record MemoryPromptWindowResult(
    string Message,
    Guid MemoryId,
    Guid ThreadId,
    Guid AnchorPromptId,
    List<MemoryPromptDto> Prompts,
    bool HasMoreBefore,
    bool HasMoreAfter,
    int ApproximateTokenCount,
    bool IsTruncated
);

public sealed record MemoryPromptDto(
    Guid PromptId,
    DateTime PromptStartTimestamp,
    string Text
);

public sealed record MemoryConversationDto(
    Guid MemoryId,
    Guid ThreadId,
    string SessionTitle,
    string Summary,
    DateTime? SummaryTimestamp,
    DateTime? FirstPromptTimestamp,
    DateTime? LastPromptTimestamp,
    List<MemoryConversationMessageDto> Messages,
    List<MemoryToolCallDto> ToolCalls
);

public sealed record MemoryConversationMessageDto(
    Guid PromptId,
    int HookIndex,
    DateTime Timestamp,
    string HookEventName,
    string Role,
    string Message,
    string PayloadJson
);

public sealed record MemoryToolCallDto(
    Guid PromptId,
    int ToolCallIndex,
    DateTime Timestamp,
    string ToolName,
    string ToolUseId,
    string Description,
    string PayloadJson
);

/// <summary>
/// One tool call found by the cross-memory tool search. It carries the owning
/// memory so a result can be opened in its conversation.
/// </summary>
public sealed record MemoryToolCallSearchItemDto(
    Guid MemoryId,
    Guid ThreadId,
    Guid PromptId,
    int ToolCallIndex,
    DateTime Timestamp,
    string ToolName,
    string ToolUseId,
    string Description,
    string PayloadJson
)
{
    public static MemoryToolCallSearchItemDto FromReadModel(
        MemoryToolCall toolCall
    ) =>
        new(
            toolCall.MemoryAggregateId.Value,
            toolCall.ThreadId.Value,
            toolCall.PromptId.Value,
            toolCall.ToolCallIndex,
            toolCall.Timestamp,
            toolCall.ToolName,
            toolCall.ToolUseId,
            toolCall.Description,
            toolCall.PayloadJson
        );
}
