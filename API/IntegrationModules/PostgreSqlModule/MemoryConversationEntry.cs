namespace PostgreSqlModule;

internal sealed class MemoryConversationEntry
{
    public Guid MemoryAggregateId { get; set; }
    public Guid PromptId { get; set; }
    public int HookIndex { get; set; }
    public Guid ThreadId { get; set; }
    public DateTime Timestamp { get; set; }
    public string HookEventName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public long? InputTokens { get; set; }
    public long? CachedInputTokens { get; set; }
    public long? CacheWriteInputTokens { get; set; }
    public long? OutputTokens { get; set; }
    public long? ReasoningOutputTokens { get; set; }
}
