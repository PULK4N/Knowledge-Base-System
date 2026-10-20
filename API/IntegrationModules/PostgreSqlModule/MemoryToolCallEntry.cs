using NpgsqlTypes;

namespace PostgreSqlModule;

internal sealed class MemoryToolCallEntry
{
    public Guid MemoryAggregateId { get; set; }
    public Guid PromptId { get; set; }
    public int ToolCallIndex { get; set; }
    public Guid ThreadId { get; set; }
    public DateTime Timestamp { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public string ToolUseId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public string InputText { get; set; } = string.Empty;
    public string OutputText { get; set; } = string.Empty;
    public NpgsqlTsVector InputSearchVector { get; set; } = null!;
    public NpgsqlTsVector OutputSearchVector { get; set; } = null!;
}
