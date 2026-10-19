using System.Text.Json;
using MemoryModule.Application.Commands;

namespace MemoryModule.API.Mapping;

public static class CodexPromptUsageMappingExtensions
{
    public static void MapTo(this JsonElement body, SetCodexPromptUsageCommand command)
    {
        command.Payload = body.Clone();
        command.InputTokens = body.GetProperty("inputTokens").GetInt64();
        command.CachedInputTokens = body.GetProperty("cachedInputTokens").GetInt64();
        command.CacheWriteInputTokens = body.GetProperty("cacheWriteInputTokens").GetInt64();
        command.OutputTokens = body.GetProperty("outputTokens").GetInt64();
        command.ReasoningOutputTokens = body.TryGetProperty("reasoningOutputTokens", out var reasoning)
            && reasoning.ValueKind != JsonValueKind.Null ? reasoning.GetInt64() : null;
    }
}
