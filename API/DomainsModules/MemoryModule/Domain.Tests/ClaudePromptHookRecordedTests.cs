using System.Text.Json;
using EventSourcing.Shared.Models;
using MemoryModule.Domain.Events;
using MemoryModule.Domain.Models;

namespace MemoryModule.Domain.Tests;

public sealed class ClaudePromptHookRecordedTests
{
    private static readonly Guid SessionId =
        Guid.Parse("019fb72e-e0c3-7452-b32b-5bbf65433c98");

    private static readonly Guid TurnId =
        Guid.Parse("019fb72e-e3c3-7093-a89d-050d309ca4ac");

    private static readonly DateTime Timestamp =
        new(2026, 8, 29, 9, 30, 0, DateTimeKind.Utc);

    private static readonly AggregateId MemoryAggregateId =
        AggregateId.FromDatabaseGuid(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
        );

    [Fact]
    public void Apply_FirstHook_SetsThreadAndCreatesPrompt()
    {
        var state = new MemoryStateData(MemoryAggregateId);
        var @event = CreateEvent("UserPromptSubmit");

        var result = @event.Apply(state, CreateExecutionInfo());

        Assert.Same(state, result);
        Assert.Equal(new ThreadId(SessionId), state.ThreadId);

        var prompt = Assert.Single(state.ChatPrompts).Value;
        Assert.Equal(new PromptId(TurnId), prompt.PromptId);
        Assert.Equal(Timestamp, prompt.PromptStartTimestamp);

        var hook = Assert.Single(prompt.PromptHookRecords);
        Assert.Equal("UserPromptSubmit", hook.HookEventName);
        Assert.Equal(
            "Remember this",
            hook.Payload.GetProperty("prompt").GetString()
        );
    }

    [Fact]
    public void Apply_SameTurn_AppendsHookToExistingPrompt()
    {
        var state = new MemoryStateData(MemoryAggregateId);
        state = (MemoryStateData)CreateEvent("UserPromptSubmit")
            .Apply(state, CreateExecutionInfo());

        CreateEvent("Stop").Apply(
            state,
            CreateExecutionInfo(Timestamp.AddMinutes(1))
        );

        var prompt = Assert.Single(state.ChatPrompts).Value;
        Assert.Equal(Timestamp, prompt.PromptStartTimestamp);
        Assert.Equal(
            ["UserPromptSubmit", "Stop"],
            prompt.PromptHookRecords.Select(hook => hook.HookEventName)
        );
    }

    private static ClaudePromptHookRecordedV1 CreateEvent(
        string hookEventName
    ) =>
        new(
            new ThreadId(SessionId),
            new PromptId(TurnId),
            hookEventName,
            JsonSerializer.SerializeToElement(
                new
                {
                    session_id = SessionId,
                    turn_id = TurnId,
                    hook_event_name = hookEventName,
                    prompt = "Remember this"
                }
            )
        );

    private static EventExecutionInfo CreateExecutionInfo(
        DateTime? timestamp = null
    ) =>
        new()
        {
            AggregateId = MemoryAggregateId,
            Timestamp = timestamp ?? Timestamp
        };
}
