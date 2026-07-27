using System.Collections.Immutable;
using EventSourcing.Shared.Models;
using SkillsModule.Domain.Events;
using SkillsModule.Domain.Models;

namespace SkillsModule.Domain.Tests;

public sealed class SkillEventTests
{
    private static readonly EventExecutionInfo EventExecutionInfo = new();

    [Fact]
    public void CreatedV1_AppliesImmutableTextAndAttachmentMetadata()
    {
        var reference = new SkillReference("Reference content");
        var attachmentId = FileId.FromDatabaseGuid(
            Guid.Parse("11111111-1111-1111-1111-111111111111")
        );
        var attachment = new Attachment
        {
            Id = attachmentId,
            Name = "example.pdf",
            Size = 1_024,
            FileType = "application/pdf",
            Extension = "pdf"
        };
        var eventData = new SkillCreatedV1(
            "skill-name",
            "Description",
            "Content",
            ImmutableArray.Create("tag"),
            ImmutableDictionary<string, SkillReference>
                .Empty
                .WithComparers(StringComparer.Ordinal)
                .Add("references/example.md", reference),
            ImmutableDictionary<FileId, Attachment>
                .Empty
                .Add(attachmentId, attachment)
        );
        var state = new SkillStateData();

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
        Assert.Same(reference, Assert.Single(state.References).Value);
        Assert.Same(attachment, Assert.Single(state.Attachments).Value);
        Assert.Equal(
            "application/pdf",
            state.Attachments[attachmentId].FileType
        );
    }

    [Fact]
    public void DetailsUpdated_ChangesDetailsAndPreservesReferencesAndAttachments()
    {
        var reference = new SkillReference("Reference content");
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
        var eventData = new SkillDetailsUpdated
        {
            Name = "updated-name",
            Description = "Updated description",
            Content = "Updated content",
            Tags = tags
        };

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
    public void ReferenceAdded_AppendsReference()
    {
        var state = CreateState();
        var eventData = new SkillReferenceAdded
        {
            RelativePath = "references/example.md",
            Content = "Reference content"
        };

        eventData.Apply(state, EventExecutionInfo);

        var reference = Assert.Single(state.References);
        Assert.Equal("references/example.md", reference.Key);
        Assert.Equal("Reference content", reference.Value.Content);
    }

    [Fact]
    public void ReferenceAdded_RemainsNonThrowingForDuplicateRelativePath()
    {
        var state = CreateState(
            (
                "references/example.md",
                new SkillReference("Original content")
            )
        );
        var eventData = new SkillReferenceAdded
        {
            RelativePath = "references/example.md",
            Content = "Duplicate content"
        };

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
        var originalReference = new SkillReference("Original content");
        var state = CreateState(("references/example.md", originalReference));
        var eventData = new SkillReferenceUpdated
        {
            RelativePath = "references/example.md",
            Content = "Updated content"
        };

        eventData.Apply(state, EventExecutionInfo);

        var updatedReference = Assert.Single(state.References);
        Assert.NotSame(originalReference, updatedReference.Value);
        Assert.Equal("references/example.md", updatedReference.Key);
        Assert.Equal("Updated content", updatedReference.Value.Content);
    }

    [Fact]
    public void ReferenceDeleted_RemovesReference()
    {
        var state = CreateState(
            (
                "references/example.md",
                new SkillReference("Reference content")
            )
        );
        var eventData = new SkillReferenceDeleted
        {
            RelativePath = "references/example.md"
        };

        eventData.Apply(state, EventExecutionInfo);

        Assert.Empty(state.References);
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
                    new SkillReferenceUpdated
                    {
                        RelativePath = "references/missing.md",
                        Content = "Content"
                    }.Apply(state, EventExecutionInfo)
            )
            : Record.Exception(
                () =>
                    new SkillReferenceDeleted
                    {
                        RelativePath = "references/missing.md"
                    }.Apply(state, EventExecutionInfo)
            );

        Assert.Null(exception);
        Assert.Empty(state.References);
    }

    private static SkillStateData CreateState() => CreateState([], []);

    private static SkillStateData CreateState(
        params (string RelativePath, SkillReference Reference)[] references
    ) => CreateState(references, []);

    private static SkillStateData CreateState(
        (string RelativePath, SkillReference Reference) reference,
        (FileId Id, Attachment Attachment) attachment
    ) => CreateState([reference], [attachment]);

    private static SkillStateData CreateState(
        (string RelativePath, SkillReference Reference)[] references,
        (FileId Id, Attachment Attachment)[] attachments
    ) =>
        new()
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
}
