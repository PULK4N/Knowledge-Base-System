using EventSourcing.Shared.Models;
using MemoryModule.Domain.Events;
using MemoryModule.Domain.Models;
using MemoryModule.Domain.Validators;

namespace MemoryModule.Domain.Tests;

public sealed class PromptModelSetTests
{
    private static readonly AggregateId MemoryAggregateId =
        AggregateId.FromDatabaseGuid(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
        );

    private static readonly PromptId PromptId =
        new(Guid.Parse("019fb72e-e3c3-7093-a89d-050d309ca4ac"));

    [Fact]
    public void Apply_SetsModelOnItsPrompt()
    {
        var state = CreateState(withPrompt: true, model: null);

        var result = new PromptModelSetV1(PromptId, "claude-opus-5-5").Apply(
            state,
            new EventExecutionInfo
            {
                AggregateId = MemoryAggregateId,
                Timestamp = DateTime.UnixEpoch
            }
        );

        Assert.Same(state, result);
        Assert.Equal("claude-opus-5-5", state.ChatPrompts[PromptId].Model);
    }

    [Theory]
    [InlineData(true, null, true)]
    [InlineData(true, "claude-sonnet-5-5", false)]
    [InlineData(false, null, true)]
    public void ModelMustNotBeSet_RejectsPromptsThatAlreadyHaveAModel(
        bool withPrompt,
        string? model,
        bool expected
    )
    {
        var result = new PromptModelMustNotBeSetValidator().Validate(
            CreateState(withPrompt, model),
            CreatePayload()
        );

        Assert.Equal(expected, result.Succeded);
        Assert.Equal(expected, result.FailureReason is null);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PromptMustExist_AcceptsOnlyModelsForRecordedPrompts(
        bool withPrompt
    )
    {
        var result = new MemoryPromptMustExistValidator().Validate(
            CreateState(withPrompt, model: null),
            CreatePayload()
        );

        Assert.Equal(withPrompt, result.Succeded);
        Assert.Equal(withPrompt, result.FailureReason is null);
    }

    private static EventPayload CreatePayload() =>
        EventPayload.Create(
            EventExecutor.FromDatabaseGuid(
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
            ),
            MemoryAggregateId,
            "memory-state-machine",
            new PromptModelSetV1(PromptId, "claude-opus-5-5")
        );

    private static MemoryStateData CreateState(bool withPrompt, string? model)
    {
        var state = new MemoryStateData(MemoryAggregateId);
        if (withPrompt)
            state.ChatPrompts.Add(
                PromptId,
                new ChatPrompt { PromptId = PromptId, Model = model }
            );

        return state;
    }
}
