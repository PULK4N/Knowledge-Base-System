namespace MemoryModule.Domain.Models;

public sealed class ChatPrompt
{
    public PromptId PromptId { get; set; }
    public DateTime PromptStartTimestamp { get; set; }
    public string? Model { get; set; }
    /// <summary>All input tokens, including cache reads and writes.</summary>
    public long? InputTokens { get; set; }
    public long? CachedInputTokens { get; set; }
    public long? CacheWriteInputTokens { get; set; }
    public long? OutputTokens { get; set; }
    /// <summary>The reasoning portion of output tokens, when reported.</summary>
    public long? ReasoningOutputTokens { get; set; }
    public List<PromptHookRecord> PromptHookRecords { get; set; } = [];
    public List<ToolCallRecord> ToolCalls { get; set; } = [];
    public List<MemoryAttachment> Attachments { get; set; } = [];
}
