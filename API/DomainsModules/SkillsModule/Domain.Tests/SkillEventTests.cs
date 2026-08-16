using System.Collections.Immutable;
using EventSourcing.Shared.Models;
using SkillsModule.Domain.Events;
using SkillsModule.Domain.Models;

namespace SkillsModule.Domain.Tests;

public sealed class SkillEventTests
{
    private static readonly EventExecutionInfo EventExecutionInfo = new();

    [Fact]
    public void CreatedV1_AppliesImmutableTextWithoutAttachments()
    {
        var reference = new SkillReference("Reference content");
        var eventData = new SkillCreatedV1(
            "skill-name",
            "Description",
            "Content",
            ImmutableArray.Create("tag"),
            ImmutableDictionary<string, SkillReference>
                .Empty
                .WithComparers(StringComparer.Ordinal)
                .Add("references/example.md", reference)
        );
        var state = new SkillStateData(EventExecutionInfo.AggregateId);

        eventData.Apply(state, EventExecutionInfo);
        var changedEvent = eventData with
        {
            Name = "changed-name",
            References = eventData.References.Add(
                "references/other.md",
                new SkillReference("Other content")
            )
        };

        Assert.Equal("skill-name", eventData.Name);
        Assert.Equal("changed-name", changedEvent.Name);
        Assert.Equal(["tag"], state.Tags);
        var storedReference = Assert.Single(state.References).Value;
        Assert.Equal(reference.Content, storedReference.Content);
        Assert.False(storedReference.LoadAutomatically);
        Assert.Empty(state.Attachments);
    }

    [Fact]
    public void DetailsUpdated_ChangesDetailsAndPreservesReferencesAndAttachments()
    {
        var reference = new SkillReference2("Reference content");
        var attachmentId = FileId.FromDatabaseGuid(
            Guid.Parse("22222222-2222-2222-2222-222222222222")
        );
        var attachment = new Attachment
        {
            Id = attachmentId,
            Name = "example.png",
            Size = 512,
            FileType = "image/png",
            Extension = "png"
        };
        var state = CreateState(
            ("references/example.md", reference),
            (attachmentId, attachment)
        );
        var tags = new List<string> { "updated" };
        var eventData = new SkillDetailsUpdatedV1(
            "updated-name",
            "Updated description",
            "Updated content",
            tags.ToImmutableArray()
        );

        eventData.Apply(state, EventExecutionInfo);
        tags.Add("changed-after-apply");

        Assert.Equal("updated-name", state.Name);
        Assert.Equal("Updated description", state.Description);
        Assert.Equal("Updated content", state.Content);
        Assert.Equal(["updated"], state.Tags);
        Assert.Same(reference, Assert.Single(state.References).Value);
        Assert.Same(attachment, Assert.Single(state.Attachments).Value);
    }

    [Fact]
    public void Updated_ReplacesSkillTextAndPreservesAttachments()
    {
        var attachment = CreateAttachment(
            "77777777-7777-7777-7777-777777777777"
        );
        var state = CreateState(
            [],
            [(attachment.Id, attachment)]
        );
        var eventData = new SkillUpdatedV1(
            "updated-name",
            "Updated description",
            "Updated content",
            ["updated"],
            ImmutableDictionary<string, SkillReference>.Empty
        );

        eventData.Apply(state, EventExecutionInfo);

        Assert.Equal("updated-name", state.Name);
        Assert.Equal(["updated"], state.Tags);
        Assert.Same(attachment, Assert.Single(state.Attachments).Value);
    }

    [Fact]
    public void ReferenceAdded_AppendsReference()
    {
        var state = CreateState();
        var eventData = new SkillReferenceAddedV2(
            "references/example.md",
            "Reference content",
            true
        );

        eventData.Apply(state, EventExecutionInfo);

        var reference = Assert.Single(state.References);
        Assert.Equal("references/example.md", reference.Key);
        Assert.Equal("Reference content", reference.Value.Content);
        Assert.True(reference.Value.LoadAutomatically);
    }

    [Fact]
    public void ReferenceAddedV1_DefaultsAutomaticLoadingToFalse()
    {
        var state = CreateState();
        var eventData = new SkillReferenceAddedV1(
            "references/example.md",
            "Reference content"
        );

        eventData.Apply(state, EventExecutionInfo);

        Assert.False(
            Assert.Single(state.References).Value.LoadAutomatically
        );
    }

    [Fact]
    public void ReferenceAdded_RemainsNonThrowingForDuplicateRelativePath()
    {
        var state = CreateState(
            (
                "references/example.md",
                new SkillReference2("Original content")
            )
        );
        var eventData = new SkillReferenceAddedV1(
            "references/example.md",
            "Duplicate content"
        );

        var exception = Record.Exception(
            () => eventData.Apply(state, EventExecutionInfo)
        );

        Assert.Null(exception);
        Assert.Equal(
            "Original content",
            Assert.Single(state.References).Value.Content
        );
    }

    [Fact]
    public void ReferenceUpdated_ReplacesReferenceContent()
    {
        var originalReference = new SkillReference2("Original content");
        var state = CreateState(("references/example.md", originalReference));
        var eventData = new SkillReferenceUpdatedV2(
            "references/example.md",
            "Updated content",
            true
        );

        eventData.Apply(state, EventExecutionInfo);

        var updatedReference = Assert.Single(state.References);
        Assert.NotSame(originalReference, updatedReference.Value);
        Assert.Equal("references/example.md", updatedReference.Key);
        Assert.Equal("Updated content", updatedReference.Value.Content);
        Assert.True(updatedReference.Value.LoadAutomatically);
    }

    [Fact]
    public void ReferenceAutoLoadUpdated_ChangesOnlyAutomaticLoading()
    {
        var originalReference = new SkillReference2(
            "Original content",
            false
        );
        var state = CreateState(("references/example.md", originalReference));
        var eventData = new SkillReferenceAutoLoadUpdatedV1(
            "references/example.md",
            true
        );

        eventData.Apply(state, EventExecutionInfo);

        var updatedReference = Assert.Single(state.References).Value;
        Assert.NotSame(originalReference, updatedReference);
        Assert.Equal("Original content", updatedReference.Content);
        Assert.True(updatedReference.LoadAutomatically);
    }

    [Fact]
    public void ReferenceDeleted_RemovesReference()
    {
        var state = CreateState(
            (
                "references/example.md",
                new SkillReference2("Reference content")
            )
        );
        var eventData = new SkillReferenceDeletedV1(
            "references/example.md"
        );

        eventData.Apply(state, EventExecutionInfo);

        Assert.Empty(state.References);
    }

    [Fact]
    public void AttachmentAdded_AppendsAttachment()
    {
        var attachment = CreateAttachment(
            "33333333-3333-3333-3333-333333333333"
        );
        var state = CreateState();

        new SkillAttachmentAddedV1(attachment).Apply(
            state,
            EventExecutionInfo
        );

        Assert.Same(
            attachment,
            Assert.Single(state.Attachments).Value
        );
    }

    [Fact]
    public void AttachmentAdded_RemainsNonThrowingForDuplicateId()
    {
        var attachment = CreateAttachment(
            "44444444-4444-4444-4444-444444444444"
        );
        var state = CreateState(
            [],
            [(attachment.Id, attachment)]
        );

        var exception = Record.Exception(
            () =>
                new SkillAttachmentAddedV1(
                    attachment with { Name = "replacement.pdf" }
                ).Apply(state, EventExecutionInfo)
        );

        Assert.Null(exception);
        Assert.Same(attachment, Assert.Single(state.Attachments).Value);
    }

    [Fact]
    public void AttachmentDeleted_RemovesAttachment()
    {
        var attachment = CreateAttachment(
            "55555555-5555-5555-5555-555555555555"
        );
        var state = CreateState(
            [],
            [(attachment.Id, attachment)]
        );

        new SkillAttachmentDeletedV1(attachment.Id).Apply(
            state,
            EventExecutionInfo
        );

        Assert.Empty(state.Attachments);
    }

    [Fact]
    public void AttachmentDeleted_RemainsNonThrowingForUnknownId()
    {
        var attachmentId = FileId.FromDatabaseGuid(
            Guid.Parse("66666666-6666-6666-6666-666666666666")
        );
        var state = CreateState();

        var exception = Record.Exception(
            () =>
                new SkillAttachmentDeletedV1(attachmentId).Apply(
                    state,
                    EventExecutionInfo
                )
        );

        Assert.Null(exception);
        Assert.Empty(state.Attachments);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReferenceChange_RemainsNonThrowingForUnknownRelativePath(bool update)
    {
        var state = CreateState();

        var exception = update
            ? Record.Exception(
                () =>
                    new SkillReferenceUpdatedV1(
                        "references/missing.md",
                        "Content"
                    ).Apply(state, EventExecutionInfo)
            )
            : Record.Exception(
                () =>
                    new SkillReferenceDeletedV1(
                        "references/missing.md"
                    ).Apply(state, EventExecutionInfo)
            );

        Assert.Null(exception);
        Assert.Empty(state.References);
    }

    private static SkillStateData CreateState() => CreateState([], []);

    private static SkillStateData CreateState(
        params (string RelativePath, SkillReference2 Reference)[] references
    ) => CreateState(references, []);

    private static SkillStateData CreateState(
        (string RelativePath, SkillReference2 Reference) reference,
        (FileId Id, Attachment Attachment) attachment
    ) => CreateState([reference], [attachment]);

    private static SkillStateData CreateState(
        (string RelativePath, SkillReference2 Reference)[] references,
        (FileId Id, Attachment Attachment)[] attachments
    ) =>
        new(EventExecutionInfo.AggregateId)
        {
            Name = "skill-name",
            Description = "Description",
            Content = "Content",
            References = references
                .ToDictionary(
                    reference => reference.RelativePath,
                    reference => reference.Reference,
                    StringComparer.Ordinal
                ),
            Attachments = attachments
                .ToDictionary(
                    attachment => attachment.Id,
                    attachment => attachment.Attachment
                )
        };

    private static Attachment CreateAttachment(string id) =>
        new()
        {
            Id = FileId.FromDatabaseGuid(Guid.Parse(id)),
            Name = "example.pdf",
            Size = 1_024,
            FileType = "application/pdf",
            Extension = "pdf"
        };
}
