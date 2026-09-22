using System.Text.Json;
using MemoryModule.API.Mapping;
using MemoryModule.Application.Commands;
using MemoryModule.Domain.Models;

namespace MemoryModule.API.Tests;

public sealed class ClaudeToolCallMappingTests
{
    private static readonly Guid SessionId =
        Guid.Parse("019fb72e-e0c3-7452-b32b-5bbf65433c98");

    private static readonly Guid TurnId =
        Guid.Parse("019fb72e-e3c3-7093-a89d-050d309ca4ac");

    [Fact]
    public void MapTo_MapsIdentityAndPreservesPayload()
    {
        var payload = CreatePayload(
            new
            {
                session_id = SessionId,
                turn_id = TurnId,
                hook_event_name = "PostToolUse",
                tool_name = "Bash",
                tool_use_id = "tool-use-1",
                tool_input = new { command = "dotnet test" },
                tool_response = new { output = "Passed" }
            }
        );
        var command = CreateCommand();

        payload.MapTo(command);

        Assert.Equal(new ThreadId(SessionId), command.ThreadId);
        Assert.Equal(new PromptId(TurnId), command.PromptId);
        Assert.Equal("Bash", command.ToolName);
        Assert.Equal("tool-use-1", command.ToolUseId);
        Assert.Equal(
            payload.GetRawText(),
            command.Payload.GetRawText()
        );
    }

    private static JsonElement CreatePayload(object value) =>
        JsonSerializer.SerializeToElement(value);

    private static RecordClaudeToolCallCommand CreateCommand() =>
        new(null!)
        {
            ThreadId = default,
            PromptId = default,
            ToolName = string.Empty,
            ToolUseId = string.Empty,
            Payload = default
        };
}
