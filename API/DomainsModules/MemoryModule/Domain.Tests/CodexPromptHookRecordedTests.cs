using System.Text.Json;
using EventSourcing.Shared.Models;
using MemoryModule.Domain.Events;
using MemoryModule.Domain.Models;

namespace MemoryModule.Domain.Tests;

public sealed class CodexPromptHookRecordedTests
{
    private static readonly Guid SessionId =
        Guid.Parse("019fb72e-e0c3-7452-b32b-5bbf65433c98");

    private static readonly Guid FirstTurnId =
        Guid.Parse("019fb72e-e3c3-7093-a89d-050d309ca4ac");

    private static readonly Guid SecondTurnId =
        Guid.Parse("019fb72e-e4c3-7093-a89d-050d309ca4ac");

    private static readonly DateTime Timestamp =
        new(2026, 7, 31, 9, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Apply_FirstHook_SetsThreadAndCreatesPrompt()
    {
        var state = new MemoryStateData();
        var payload = CreatePayload(SessionId, FirstTurnId);
        var @event = new CodexPromptHookRecordedV1(
            new ThreadId(SessionId),
            new PromptId(FirstTurnId),
            "after_agent",
            payload
        );
        var executionInfo = CreateExecutionInfo(Timestamp);

        var result = @event.Apply(state, executionInfo);

        Assert.Same(state, result);
        Assert.Equal(executionInfo.AggregateId, state.Id);
        Assert.False(state.IsDeleted);
        Assert.Equal(new ThreadId(SessionId), state.ThreadId);

        var prompt = Assert.Single(state.ChatPrompts).Value;
        Assert.Equal(new PromptId(FirstTurnId), prompt.PromptId);
        Assert.Equal(Timestamp, prompt.PromptStartTimestamp);

        var hook = Assert.Single(prompt.PromptHookRecords);
        Assert.Equal("after_agent", hook.HookEventName);
        Assert.Equal(
            FirstTurnId.ToString(),
            hook.Payload.GetProperty("turn_id").GetString()
        );
    }

    [Fact]
    public void Apply_ExistingPrompt_AppendsHook()
    {
        var promptId = new PromptId(FirstTurnId);
        var firstTimestamp = Timestamp.AddMinutes(-1);
        var state = new MemoryStateData
        {
            ThreadId = new ThreadId(SessionId),
            ChatPrompts =
            {
                [promptId] = new ChatPrompt
                {
                    PromptId = promptId,
                    PromptStartTimestamp = firstTimestamp,
                    PromptHookRecords =
                    {
                        new()
                        {
                            HookEventName = "before_agent",
                            Payload = CreatePayload(SessionId, FirstTurnId)
                        }
                    }
                }
            }
        };
        var @event = new CodexPromptHookRecordedV1(
            new ThreadId(SessionId),
            promptId,
            "after_agent",
            CreatePayload(SessionId, FirstTurnId)
        );

        @event.Apply(state, CreateExecutionInfo(Timestamp));

        var prompt = state.ChatPrompts[promptId];
        Assert.Equal(firstTimestamp, prompt.PromptStartTimestamp);
        Assert.Equal(2, prompt.PromptHookRecords.Count);
        Assert.Equal(
            "after_agent",
            prompt.PromptHookRecords[1].HookEventName
        );
    }

    [Fact]
    public void Apply_NewTurnInExistingThread_CreatesAnotherPrompt()
    {
        var firstPromptId = new PromptId(FirstTurnId);
        var state = new MemoryStateData
        {
            ThreadId = new ThreadId(SessionId),
            ChatPrompts =
            {
                [firstPromptId] = new ChatPrompt
                {
                    PromptId = firstPromptId,
                    PromptStartTimestamp = Timestamp.AddMinutes(-1)
                }
            }
        };
        var @event = new CodexPromptHookRecordedV1(
            new ThreadId(SessionId),
            new PromptId(SecondTurnId),
            "before_agent",
            CreatePayload(SessionId, SecondTurnId)
        );

        @event.Apply(state, CreateExecutionInfo(Timestamp));

        Assert.Equal(2, state.ChatPrompts.Count);
        var prompt = state.ChatPrompts[new PromptId(SecondTurnId)];
        Assert.Equal(Timestamp, prompt.PromptStartTimestamp);
        Assert.Single(prompt.PromptHookRecords);
    }

    [Fact]
    public void Apply_UsesIdsFromEventWithoutReadingPayload()
    {
        var state = new MemoryStateData();
        var @event = new CodexPromptHookRecordedV1(
            new ThreadId(SessionId),
            new PromptId(FirstTurnId),
            "after_agent",
            JsonSerializer.SerializeToElement(new { value = "hook-data" })
        );

        @event.Apply(state, CreateExecutionInfo(Timestamp));

        Assert.Equal(new ThreadId(SessionId), state.ThreadId);
        Assert.True(
            state.ChatPrompts.ContainsKey(new PromptId(FirstTurnId))
        );
    }

    private static JsonElement CreatePayload(Guid sessionId, Guid turnId) =>
        JsonSerializer.SerializeToElement(
            new
            {
                session_id = sessionId,
                turn_id = turnId
            }
        );

    private static EventExecutionInfo CreateExecutionInfo(
        DateTime timestamp
    ) =>
        new()
        {
            AggregateId = AggregateId.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            ),
            Timestamp = timestamp
        };
}
