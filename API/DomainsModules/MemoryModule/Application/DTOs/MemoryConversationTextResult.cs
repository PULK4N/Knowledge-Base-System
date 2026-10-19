namespace MemoryModule.Application.DTOs;

public sealed record MemoryConversationTextResult(
    Guid MemoryId,
    int Offset,
    int Limit,
    int TotalCount,
    bool HasMore,
    List<MemoryConversationTextMessageDto> Messages,
    long? InputTokens = null,
    long? CachedInputTokens = null,
    long? CacheWriteInputTokens = null,
    long? OutputTokens = null,
    long? ReasoningOutputTokens = null
)
{
    public long? TotalTokens => InputTokens + OutputTokens;
}
