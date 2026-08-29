using System.Text.Json;
using MemoryModule.Domain.Models;

namespace MemoryModule.Domain.Tests;

public sealed class PromptHookPayloadTests
{
    [Theory]
    [InlineData(
        """{"hook_event_name":"UserPromptSubmit","prompt":"Remember this","session_id":"019f"}""",
        "Remember this"
    )]
    [InlineData(
        """{"hook_event_name":"Stop","last_assistant_message":"Implemented it."}""",
        "Implemented it."
    )]
    [InlineData(
        """{"prompt":"Asked first","last_assistant_message":"Answered second"}""",
        "Asked first"
    )]
    [InlineData("""{"session_id":"019f","cwd":"/repository"}""", null)]
    [InlineData("""{"prompt":"   "}""", null)]
    [InlineData("""{"prompt":{"text":"Structured"}}""", null)]
    [InlineData("""["prompt"]""", null)]
    public void FindMessage_returns_the_conversation_text_of_a_hook(
        string payloadJson,
        string? expected
    )
    {
        var payload = JsonDocument.Parse(payloadJson).RootElement;

        Assert.Equal(expected, PromptHookPayload.FindMessage(payload));
    }
}
