using System.Text.Json;
using EventSourcing.Shared.Models;
using MemoryModule.Domain.Events;
using MemoryModule.Domain.Models;

namespace MemoryModule.Domain.Tests;

public sealed class CodexMemoryMigratedTests
{
    [Fact]
    public void V1_RoundTripsAndStoresSingleArbitraryPrompt()
    {
        var threadId = new ThreadId(
            Guid.Parse("019fb72e-e0c3-7452-b32b-5bbf65433c98")
        );
        var promptId = new PromptId(
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")
        );
        var timestamp = new DateTime(
            2026,
            7,
            31,
            9,
            30,
            0,
            DateTimeKind.Utc
        );
        var @event = new CodexMemoryMigratedV1(
            threadId,
            promptId,
            JsonSerializer.SerializeToElement(
                new
                {
                    raw_memory = "Raw stage-one memory",
                    rollout_summary = "Thread rollout summary",
                    source = "codex-stage1-output"
                }
            )
        );
        var deserialized = Assert.IsType<CodexMemoryMigratedV1>(
            JsonSerializer.Deserialize<CodexMemoryMigratedV1>(
                JsonSerializer.Serialize(@event)
            )
        );
        var state = new MemoryStateData(
            AggregateId.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            )
        );

        var result = deserialized.Apply(
            state,
            new EventExecutionInfo
            {
                Timestamp = timestamp
            }
        );

        Assert.Same(state, result);
        Assert.Equal(threadId, state.ThreadId);

        var prompt = Assert.Single(state.ChatPrompts).Value;
        Assert.Equal(promptId, prompt.PromptId);
        Assert.Equal(timestamp, prompt.PromptStartTimestamp);

        var hook = Assert.Single(prompt.PromptHookRecords);
        Assert.Equal(
            CodexMemoryMigratedV1.UserMigrationHookEventName,
            hook.HookEventName
        );
        Assert.Equal(3, hook.Payload.EnumerateObject().Count());
        Assert.Equal(
            "Raw stage-one memory",
            hook.Payload.GetProperty("raw_memory").GetString()
        );
        Assert.Equal(
            "Thread rollout summary",
            hook.Payload.GetProperty("rollout_summary").GetString()
        );
        Assert.Equal(
            "codex-stage1-output",
            hook.Payload.GetProperty("source").GetString()
        );
    }
}
