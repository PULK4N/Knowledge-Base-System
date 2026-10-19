using System.Text.Json;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using MemoryModule.Domain.Events;
using MemoryModule.Domain.Models;
using MemoryModule.Domain.Validators;

namespace MemoryModule.Domain.Tests;

public sealed class PromptUsageSetTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Apply_normalizes_usage_and_replaces_previous_counts(bool claude)
    {
        var promptId = new PromptId(Guid.NewGuid());
        var state = new MemoryStateData(AggregateId.FromDatabaseGuid(Guid.NewGuid()));
        state.ChatPrompts.Add(promptId, new ChatPrompt { PromptId = promptId });
        IEvent usage = claude
            ? new ClaudePromptUsageSetV1(promptId, 2, 25747, 15466, 504, 274)
            : new CodexPromptUsageSetV1(promptId, 41215, 25747, 15466, 504, 274);
        var restored = (IEvent)JsonSerializer.Deserialize(
            JsonSerializer.Serialize(usage, usage.GetType()), usage.GetType())!;

        restored.Apply(state, new EventExecutionInfo());
        restored.Apply(state, new EventExecutionInfo());

        var prompt = state.ChatPrompts[promptId];
        Assert.Equal(41215L, prompt.InputTokens);
        Assert.Equal(25747L, prompt.CachedInputTokens);
        Assert.Equal(15466L, prompt.CacheWriteInputTokens);
        Assert.Equal(504L, prompt.OutputTokens);
        Assert.Equal(274L, prompt.ReasoningOutputTokens);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void Usage_requires_an_existing_prompt(bool claude, bool exists)
    {
        var promptId = new PromptId(Guid.NewGuid());
        var id = AggregateId.FromDatabaseGuid(Guid.NewGuid());
        var state = new MemoryStateData(id);
        if (exists)
            state.ChatPrompts.Add(promptId, new ChatPrompt { PromptId = promptId });
        var payload = Payload(id, claude
            ? new ClaudePromptUsageSetV1(promptId, 0, 0, 0, 0)
            : new CodexPromptUsageSetV1(promptId, 0, 0, 0, 0));
        Assert.Equal(exists, new MemoryPromptMustExistValidator().Validate(state, payload).Succeded);
    }

    [Theory]
    [InlineData(true, -1, 0, false)]
    [InlineData(false, -1, 0, false)]
    [InlineData(true, 0, null, true)]
    [InlineData(false, 0, null, true)]
    [InlineData(true, 10, 11, false)]
    [InlineData(false, 10, 11, false)]
    [InlineData(true, 10, 10, true)]
    [InlineData(false, 10, 10, true)]
    public void Usage_validates_counts(bool claude, long output, long? reasoning, bool expected)
    {
        var promptId = new PromptId(Guid.NewGuid());
        var id = AggregateId.FromDatabaseGuid(Guid.NewGuid());
        var payload = Payload(id, claude
            ? new ClaudePromptUsageSetV1(promptId, 10, 0, 0, output, reasoning)
            : new CodexPromptUsageSetV1(promptId, 10, 0, 0, output, reasoning));
        Assert.Equal(expected, new PromptUsageMustBeValidValidator().Validate(new MemoryStateData(id), payload).Succeded);
    }

    private static EventPayload Payload(AggregateId id, IEvent usage) =>
        EventPayload.Create(EventExecutor.FromDatabaseGuid(Guid.NewGuid()), id, "memory-state-machine", usage);
}
