using System.Text.Json;
using EventSourcing.Shared.Models;
using MemoryModule.Domain.Events;
using MemoryModule.Domain.Models;

namespace MemoryModule.Domain.Tests;

public sealed class CodexToolCallRecordedTests
{
    private static readonly Guid SessionId =
        Guid.Parse("019fb72e-e0c3-7452-b32b-5bbf65433c98");

    private static readonly Guid TurnId =
        Guid.Parse("019fb72e-e3c3-7093-a89d-050d309ca4ac");

    private static readonly DateTime Timestamp =
        new(2026, 7, 31, 9, 30, 0, DateTimeKind.Utc);

    private static readonly AggregateId MemoryAggregateId =
        AggregateId.FromDatabaseGuid(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
        );

    [Fact]
    public void Apply_FirstToolCall_SetsThreadAndCreatesPromptToolCall()
    {
        var state = new MemoryStateData(MemoryAggregateId);
        var payload = CreatePayload();
        var @event = new CodexToolCallRecordedV1(
            new ThreadId(SessionId),
            new PromptId(TurnId),
            "Bash",
            "tool-use-1",
            payload
        );

        var result = @event.Apply(state, CreateExecutionInfo());

        Assert.Same(state, result);
        Assert.Equal(new ThreadId(SessionId), state.ThreadId);
        var prompt = Assert.Single(state.ChatPrompts).Value;
        Assert.Equal(Timestamp, prompt.PromptStartTimestamp);
        var toolCall = Assert.Single(prompt.CodexToolCalls);
        Assert.Equal("Bash", toolCall.ToolName);
        Assert.Equal("tool-use-1", toolCall.ToolUseId);
        Assert.Equal(
            "Bash",
            toolCall.Payload.GetProperty("tool_name").GetString()
        );
        Assert.Equal(
            "dotnet test",
            toolCall.Payload
                .GetProperty("tool_input")
                .GetProperty("command")
                .GetString()
        );
        Assert.Equal(
            "Passed",
            toolCall.Payload
                .GetProperty("tool_response")
                .GetProperty("output")
                .GetString()
        );
    }

    [Fact]
    public void Apply_ExistingPrompt_AppendsToolCallWithoutChangingPromptStart()
    {
        var promptId = new PromptId(TurnId);
        var firstTimestamp = Timestamp.AddMinutes(-1);
        var state = new MemoryStateData(MemoryAggregateId)
        {
            ThreadId = new ThreadId(SessionId),
            ChatPrompts =
            {
                [promptId] = new ChatPrompt
                {
                    PromptId = promptId,
                    PromptStartTimestamp = firstTimestamp
                }
            }
        };

        new CodexToolCallRecordedV1(
            new ThreadId(SessionId),
            promptId,
            "Bash",
            "tool-use-1",
            CreatePayload()
        ).Apply(state, CreateExecutionInfo());

        var prompt = state.ChatPrompts[promptId];
        Assert.Equal(firstTimestamp, prompt.PromptStartTimestamp);
        Assert.Single(prompt.CodexToolCalls);
    }

    private static JsonElement CreatePayload() =>
        JsonSerializer.SerializeToElement(
            new
            {
                session_id = SessionId,
                turn_id = TurnId,
                tool_name = "Bash",
                tool_use_id = "tool-use-1",
                tool_input = new { command = "dotnet test" },
                tool_response = new { output = "Passed" }
            }
        );

    private static EventExecutionInfo CreateExecutionInfo() =>
        new()
        {
            AggregateId = MemoryAggregateId,
            Timestamp = Timestamp
        };
}
