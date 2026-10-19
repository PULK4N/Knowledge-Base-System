using System.Text.Json;
using MemoryModule.API.Mapping;
using MemoryModule.Application.Commands;

namespace MemoryModule.API.Tests;

public sealed class PromptUsageMappingTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Mapping_preserves_full_json_after_request_document_is_disposed(bool claude)
    {
        JsonElement retained;
        long input;
        using (var document = JsonDocument.Parse("""
            {"inputTokens":10,"cacheReadInputTokens":2,"cacheCreationInputTokens":3,
             "cachedInputTokens":2,"cacheWriteInputTokens":3,"outputTokens":4,
             "thinkingTokens":null,"reasoningOutputTokens":null,
             "sourceRecords":[{"message":{"usage":{"extraProviderField":42}}}],
             "extraRequestField":"preserved"}
            """))
        {
            if (claude)
            {
                var command = new SetClaudePromptUsageCommand(null!)
                {
                    ThreadId = default, PromptId = default, Payload = default,
                    InputTokens = 0, CacheReadInputTokens = 0,
                    CacheCreationInputTokens = 0, OutputTokens = 0
                };
                document.RootElement.MapTo(command);
                retained = command.Payload;
                input = command.InputTokens;
                Assert.Null(command.ThinkingTokens);
            }
            else
            {
                var command = new SetCodexPromptUsageCommand(null!)
                {
                    ThreadId = default, PromptId = default, Payload = default,
                    InputTokens = 0, CachedInputTokens = 0,
                    CacheWriteInputTokens = 0, OutputTokens = 0
                };
                document.RootElement.MapTo(command);
                retained = command.Payload;
                input = command.InputTokens;
                Assert.Null(command.ReasoningOutputTokens);
            }
            Assert.Equal(document.RootElement.GetRawText(), retained.GetRawText());
        }
        Assert.Equal(10, input);
        Assert.Equal("preserved", retained.GetProperty("extraRequestField").GetString());
        Assert.Equal(42, retained.GetProperty("sourceRecords")[0].GetProperty("message")
            .GetProperty("usage").GetProperty("extraProviderField").GetInt32());
    }
}
