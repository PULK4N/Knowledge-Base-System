namespace MemoryModule.Application.DTOs;

public sealed record MemoryConversationTextMessageDto(
    Guid PromptId,
    DateTime Timestamp,
    string Role,
    string Text,
    long? InputTokens = null,
    long? CachedInputTokens = null,
    long? CacheWriteInputTokens = null,
    long? OutputTokens = null,
    long? ReasoningOutputTokens = null
)
{
    public long? TotalTokens => InputTokens + OutputTokens;
}
