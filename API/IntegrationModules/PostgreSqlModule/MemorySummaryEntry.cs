namespace PostgreSqlModule;

internal sealed class MemorySummaryEntry
{
    public Guid MemoryAggregateId { get; set; }
    public Guid ThreadId { get; set; }
    public string SessionTitle { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int PromptCount { get; set; }
    public DateTime? FirstPromptTimestamp { get; set; }
    public DateTime? LastPromptTimestamp { get; set; }
    public DateTime? SummaryTimestamp { get; set; }
    public DateTime LastActivityTimestamp { get; set; }
    public List<string> Models { get; set; } = [];
    public long? InputTokens { get; set; }
    public long? CachedInputTokens { get; set; }
    public long? CacheWriteInputTokens { get; set; }
    public long? OutputTokens { get; set; }
    public long? ReasoningOutputTokens { get; set; }
}
