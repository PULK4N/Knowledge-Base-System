using System.Text.Json;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using MemoryModule.Domain.Events;
using MemoryModule.Domain.Models;
using MemoryModule.Domain.Validators;

namespace MemoryModule.Domain.Tests;

public sealed class PromptUsageSetTests
{
    private static JsonElement SourcePayload => JsonSerializer.SerializeToElement(new { sourceRecords = new[] { new { type = "usage", unknownField = 42 } } });
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Apply_normalizes_usage_and_replaces_previous_counts(bool claude)
    {
        var promptId = new PromptId(Guid.NewGuid());
        var state = new MemoryStateData(AggregateId.FromDatabaseGuid(Guid.NewGuid()));
        state.ChatPrompts.Add(promptId, new ChatPrompt { PromptId = promptId });
        IEvent usage = claude
            ? new ClaudePromptUsageSetV2(promptId, 2, 25747, 15466, 504, SourcePayload, 274)
            : new CodexPromptUsageSetV2(promptId, 41215, 25747, 15466, 504, SourcePayload, 274);
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
        Assert.Equal(SourcePayload.GetRawText(), prompt.UsagePayload!.Value.GetRawText());
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
            ? new ClaudePromptUsageSetV2(promptId, 0, 0, 0, 0, SourcePayload)
            : new CodexPromptUsageSetV2(promptId, 0, 0, 0, 0, SourcePayload));
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
            ? new ClaudePromptUsageSetV2(promptId, 10, 0, 0, output, SourcePayload, reasoning)
            : new CodexPromptUsageSetV2(promptId, 10, 0, 0, output, SourcePayload, reasoning));
        Assert.Equal(expected, new PromptUsageMustBeValidValidator().Validate(new MemoryStateData(id), payload).Succeded);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Usage_is_write_once_even_for_zero_or_partial_counts(bool claude)
    {
        var id = AggregateId.FromDatabaseGuid(Guid.NewGuid());
        var promptId = new PromptId(Guid.NewGuid());
        var state = new MemoryStateData(id);
        var prompt = new ChatPrompt { PromptId = promptId };
        state.ChatPrompts.Add(promptId, prompt);
        var payload = Payload(id, claude
            ? new ClaudePromptUsageSetV2(promptId, 10, 0, 0, 5, SourcePayload)
            : new CodexPromptUsageSetV2(promptId, 10, 0, 0, 5, SourcePayload));
        var validator = new PromptUsageMustNotBeSetValidator();
        Assert.True(validator.Validate(state, payload).Succeded);
        foreach (var property in new List<string>
        {
            nameof(ChatPrompt.InputTokens), nameof(ChatPrompt.CachedInputTokens),
            nameof(ChatPrompt.CacheWriteInputTokens), nameof(ChatPrompt.OutputTokens),
            nameof(ChatPrompt.ReasoningOutputTokens)
        })
        {
            var member = typeof(ChatPrompt).GetProperty(property)!;
            member.SetValue(prompt, 0L);
            Assert.False(validator.Validate(state, payload).Succeded);
            member.SetValue(prompt, null);
        }
        payload.EventData.Apply(state, new EventExecutionInfo());
        Assert.False(validator.Validate(state, payload).Succeded);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Legacy_usage_replays_without_counts_and_allows_v2(bool claude)
    {
        var id = AggregateId.FromDatabaseGuid(Guid.NewGuid());
        var promptId = new PromptId(Guid.NewGuid());
        var state = new MemoryStateData(id);
        state.ChatPrompts.Add(promptId, new ChatPrompt { PromptId = promptId });
        IEvent legacy = claude
            ? new ClaudePromptUsageSetV1(promptId, 10, 2, 3, 5, 1)
            : new CodexPromptUsageSetV1(promptId, 15, 2, 3, 5, 1);
        var restored = (IEvent)JsonSerializer.Deserialize(
            JsonSerializer.Serialize(legacy, legacy.GetType()), legacy.GetType())!;
        restored.Apply(state, new EventExecutionInfo());
        var prompt = state.ChatPrompts[promptId];
        Assert.Null(prompt.InputTokens);
        Assert.Null(prompt.CachedInputTokens);
        Assert.Null(prompt.CacheWriteInputTokens);
        Assert.Null(prompt.OutputTokens);
        Assert.Null(prompt.ReasoningOutputTokens);
        Assert.Null(prompt.UsagePayload);
        IEvent current = claude
            ? new ClaudePromptUsageSetV2(promptId, 10, 2, 3, 5, SourcePayload, 1)
            : new CodexPromptUsageSetV2(promptId, 15, 2, 3, 5, SourcePayload, 1);
        var payload = Payload(id, current);
        var validator = new PromptUsageMustNotBeSetValidator();
        Assert.True(validator.Validate(state, payload).Succeded);
        current.Apply(state, new EventExecutionInfo());
        restored.Apply(state, new EventExecutionInfo());
        Assert.Equal(15L, prompt.InputTokens);
        Assert.Equal(SourcePayload.GetRawText(), prompt.UsagePayload!.Value.GetRawText());
        Assert.False(validator.Validate(state, payload).Succeded);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void V2_requires_original_source_records(bool claude)
    {
        var id = AggregateId.FromDatabaseGuid(Guid.NewGuid());
        var promptId = new PromptId(Guid.NewGuid());
        foreach (var json in new List<string> { "{}", "{\"sourceRecords\":[]}", "{\"sourceRecords\":[null]}" })
        {
            var source = JsonSerializer.Deserialize<JsonElement>(json);
            IEvent usage = claude
                ? new ClaudePromptUsageSetV2(promptId, 0, 0, 0, 0, source)
                : new CodexPromptUsageSetV2(promptId, 0, 0, 0, 0, source);
            Assert.False(new PromptUsageMustBeValidValidator().Validate(new MemoryStateData(id), Payload(id, usage)).Succeded);
        }
    }

    private static EventPayload Payload(AggregateId id, IEvent usage) =>
        EventPayload.Create(EventExecutor.FromDatabaseGuid(Guid.NewGuid()), id, "memory-state-machine", usage);
}
