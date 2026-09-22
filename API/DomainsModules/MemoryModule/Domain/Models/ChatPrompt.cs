namespace MemoryModule.Domain.Models;

public sealed class ChatPrompt
{
    public PromptId PromptId { get; set; }
    public DateTime PromptStartTimestamp { get; set; }
    public List<PromptHookRecord> PromptHookRecords { get; set; } = [];
    public List<ToolCallRecord> ToolCalls { get; set; } = [];
}
