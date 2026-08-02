using EventSourcing.Shared.Models;
using SkillsModule.Domain.Events;
using SkillsModule.Domain.Models;
using SkillsModule.Domain.Validators;

namespace SkillsModule.Domain.Tests;

public sealed class SkillAttachmentValidatorTests
{
    private static readonly Attachment Attachment =
        new()
        {
            Id = FileId.FromDatabaseGuid(
                Guid.Parse("11111111-1111-1111-1111-111111111111")
            ),
            Name = "example.pdf",
            Size = 1_024,
            FileType = "application/pdf",
            Extension = "pdf"
        };

    [Fact]
    public void MustNotExist_FailsForDuplicateAttachmentId()
    {
        var state = CreateState(Attachment);
        var validator = new SkillAttachmentMustNotExistValidator();

        var result = validator.Validate(
            state,
            CreatePayload(new SkillAttachmentAddedV1(Attachment))
        );

        Assert.False(result.Succeded);
        Assert.Contains("already exists", result.FailureReason);
    }

    [Fact]
    public void MustNotExist_SucceedsForNewAttachmentId()
    {
        var validator = new SkillAttachmentMustNotExistValidator();

        var result = validator.Validate(
            CreateState(),
            CreatePayload(new SkillAttachmentAddedV1(Attachment))
        );

        Assert.True(result.Succeded);
        Assert.Null(result.FailureReason);
    }

    [Fact]
    public void MustExist_FailsForUnknownAttachmentId()
    {
        var validator = new SkillAttachmentMustExistValidator();

        var result = validator.Validate(
            CreateState(),
            CreatePayload(
                new SkillAttachmentDeletedV1(Attachment.Id)
            )
        );

        Assert.False(result.Succeded);
        Assert.Contains("does not exist", result.FailureReason);
    }

    [Fact]
    public void MustExist_SucceedsForKnownAttachmentId()
    {
        var validator = new SkillAttachmentMustExistValidator();

        var result = validator.Validate(
            CreateState(Attachment),
            CreatePayload(
                new SkillAttachmentDeletedV1(Attachment.Id)
            )
        );

        Assert.True(result.Succeded);
        Assert.Null(result.FailureReason);
    }

    private static SkillStateData CreateState(
        params Attachment[] attachments
    ) =>
        new(AggregateId.FromDatabaseGuid(Guid.Empty))
        {
            Name = "skill-name",
            Attachments = attachments.ToDictionary(
                attachment => attachment.Id
            )
        };

    private static EventPayload CreatePayload(
        EventSourcing.Shared.Interfaces.IEvent eventData
    ) =>
        EventPayload.Create(
            EventExecutor.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            ),
            AggregateId.FromDatabaseGuid(
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
            ),
            "skills-state-machine",
            eventData
        );
}
