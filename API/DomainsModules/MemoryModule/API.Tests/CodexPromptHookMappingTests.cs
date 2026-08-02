using System.Text.Json;
using MemoryModule.API.Mapping;
using MemoryModule.Application.Commands;
using MemoryModule.Domain.Models;

namespace MemoryModule.API.Tests;

public sealed class CodexPromptHookMappingTests
{
    [Fact]
    public void MapTo_MapsIdentityAndPreservesPayload()
    {
        var sessionId = Guid.Parse(
            "019fb72e-e0c3-7452-b32b-5bbf65433c98"
        );
        var turnId = Guid.Parse(
            "019fb72e-e3c3-7093-a89d-050d309ca4ac"
        );
        var payload = JsonSerializer.SerializeToElement(
            new
            {
                session_id = sessionId,
                turn_id = turnId,
                hook_event_name = "UserPromptSubmit",
                prompt = "Remember this"
            }
        );
        var command = new RecordCodexPromptHookCommand(null!)
        {
            ThreadId = default,
            PromptId = default,
            HookEventName = string.Empty,
            Payload = default
        };

        payload.MapTo(command);

        Assert.Equal(new ThreadId(sessionId), command.ThreadId);
        Assert.Equal(new PromptId(turnId), command.PromptId);
        Assert.Equal("UserPromptSubmit", command.HookEventName);
        Assert.Equal(
            payload.GetRawText(),
            command.Payload.GetRawText()
        );
    }
}
