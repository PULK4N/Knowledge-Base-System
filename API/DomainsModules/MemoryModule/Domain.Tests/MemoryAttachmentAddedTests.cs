using EventSourcing.Shared.Models;
using MemoryModule.Domain.Events;
using MemoryModule.Domain.Models;
using MemoryModule.Domain.Validators;

namespace MemoryModule.Domain.Tests;

public sealed class MemoryAttachmentAddedTests
{
    private static readonly AggregateId MemoryAggregateId =
        AggregateId.FromDatabaseGuid(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
        );

    private static readonly PromptId PromptId =
        new(Guid.Parse("019fb72e-e3c3-7093-a89d-050d309ca4ac"));

    private static readonly MemoryAttachment Attachment =
        new()
        {
            Id = new AttachmentId(
                Guid.Parse("11111111-1111-1111-1111-111111111111")
            ),
            Name = "image-2.png",
            Size = 1_024,
            FileType = "image/png",
            Extension = "png",
            Placeholder = "[Image #2]"
        };

    [Fact]
    public void Apply_AddsAttachmentToItsPrompt()
    {
        var state = CreateState(withPrompt: true);

        var result = new MemoryAttachmentAddedV1(PromptId, Attachment).Apply(
            state,
            new EventExecutionInfo
            {
                AggregateId = MemoryAggregateId,
                Timestamp = DateTime.UnixEpoch
            }
        );

        Assert.Same(state, result);
        Assert.Equal(
            Attachment,
            Assert.Single(state.ChatPrompts[PromptId].Attachments)
        );
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PromptMustExist_AcceptsOnlyAttachmentsForRecordedPrompts(
        bool withPrompt
    )
    {
        var result = new MemoryPromptMustExistValidator().Validate(
            CreateState(withPrompt),
            EventPayload.Create(
                EventExecutor.FromDatabaseGuid(
                    Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
                ),
                MemoryAggregateId,
                "memory-state-machine",
                new MemoryAttachmentAddedV1(PromptId, Attachment)
            )
        );

        Assert.Equal(withPrompt, result.Succeded);
        Assert.Equal(withPrompt, result.FailureReason is null);
    }

    private static MemoryStateData CreateState(bool withPrompt)
    {
        var state = new MemoryStateData(MemoryAggregateId);
        if (withPrompt)
            state.ChatPrompts.Add(PromptId, new ChatPrompt { PromptId = PromptId });

        return state;
    }
}
