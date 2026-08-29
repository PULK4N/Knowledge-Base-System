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
    string Summary,
    DateTime? SummaryTimestamp,
    DateTime? FirstPromptTimestamp,
    DateTime? LastPromptTimestamp,
    List<MemoryConversationMessageDto> Messages
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
