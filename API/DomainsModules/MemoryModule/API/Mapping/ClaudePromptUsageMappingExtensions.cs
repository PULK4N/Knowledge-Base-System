using System.Text.Json;
using MemoryModule.Application.Commands;

namespace MemoryModule.API.Mapping;

public static class ClaudePromptUsageMappingExtensions
{
    public static void MapTo(this JsonElement body, SetClaudePromptUsageCommand command)
    {
        command.Payload = body.Clone();
        command.InputTokens = body.GetProperty("inputTokens").GetInt64();
        command.CacheReadInputTokens = body.GetProperty("cacheReadInputTokens").GetInt64();
        command.CacheCreationInputTokens = body.GetProperty("cacheCreationInputTokens").GetInt64();
        command.OutputTokens = body.GetProperty("outputTokens").GetInt64();
        command.ThinkingTokens = body.TryGetProperty("thinkingTokens", out var reasoning)
            && reasoning.ValueKind != JsonValueKind.Null ? reasoning.GetInt64() : null;
    }
}
