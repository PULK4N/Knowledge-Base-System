namespace PostgreSqlModule;

internal sealed class MemorySummaryEntry
{
    public Guid MemoryAggregateId { get; set; }
    public Guid ThreadId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public int PromptCount { get; set; }
    public DateTime? FirstPromptTimestamp { get; set; }
    public DateTime? LastPromptTimestamp { get; set; }
    public DateTime? SummaryTimestamp { get; set; }
    public DateTime LastActivityTimestamp { get; set; }
}
