using System.Text.Json;

namespace MemoryModule.Domain.Models;

public sealed class PromptHookRecord
{
    public string HookEventName { get; set; } = string.Empty;
    public JsonElement Payload { get; set; }
}
