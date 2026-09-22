using System.Text.Json;

namespace MemoryModule.Domain.Models;

public sealed class ToolCallRecord
{
    public string ToolName { get; set; } = string.Empty;
    public string ToolUseId { get; set; } = string.Empty;
    public JsonElement Payload { get; set; }
}
