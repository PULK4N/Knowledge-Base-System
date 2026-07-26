using EventSourcing.Shared.Models;
using SkillsModule.Domain.Events;
using SkillsModule.Domain.Models;

namespace SkillsModule.Domain.Tests;

public sealed class SkillEventTests
{
    private static readonly EventExecutionInfo EventExecutionInfo = new();

    [Fact]
    public void DetailsUpdated_ChangesDetailsAndPreservesReferences()
    {
        var reference = new SkillReference
        {
            RelativePath = "references/example.md",
            Content = "Reference content"
        };
        var state = CreateState(reference);
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

        Assert.Equal("updated-name", state.Skill.Name);
        Assert.Equal("Updated description", state.Skill.Description);
        Assert.Equal("Updated content", state.Skill.Content);
        Assert.Equal(["updated"], state.Skill.Tags);
        Assert.Same(reference, Assert.Single(state.Skill.References));
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

        var reference = Assert.Single(state.Skill.References);
        Assert.Equal("references/example.md", reference.RelativePath);
        Assert.Equal("Reference content", reference.Content);
    }

    [Fact]
    public void ReferenceAdded_RejectsDuplicateRelativePath()
    {
        var state = CreateState(
            new SkillReference
            {
                RelativePath = "references/example.md",
                Content = "Original content"
            }
        );
        var eventData = new SkillReferenceAdded
        {
            RelativePath = "references/example.md",
            Content = "Duplicate content"
        };

        Assert.Throws<InvalidOperationException>(
            () => eventData.Apply(state, EventExecutionInfo)
        );
    }

    [Fact]
    public void ReferenceUpdated_ReplacesReferenceContent()
    {
        var originalReference = new SkillReference
        {
            RelativePath = "references/example.md",
            Content = "Original content"
        };
        var state = CreateState(originalReference);
        var eventData = new SkillReferenceUpdated
        {
            RelativePath = "references/example.md",
            Content = "Updated content"
        };

        eventData.Apply(state, EventExecutionInfo);

        var updatedReference = Assert.Single(state.Skill.References);
        Assert.NotSame(originalReference, updatedReference);
        Assert.Equal("references/example.md", updatedReference.RelativePath);
        Assert.Equal("Updated content", updatedReference.Content);
    }

    [Fact]
    public void ReferenceDeleted_RemovesReference()
    {
        var state = CreateState(
            new SkillReference
            {
                RelativePath = "references/example.md",
                Content = "Reference content"
            }
        );
        var eventData = new SkillReferenceDeleted
        {
            RelativePath = "references/example.md"
        };

        eventData.Apply(state, EventExecutionInfo);

        Assert.Empty(state.Skill.References);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReferenceChange_RejectsUnknownRelativePath(bool update)
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

        Assert.IsType<InvalidOperationException>(exception);
    }

    private static SkillStateData CreateState(params SkillReference[] references) =>
        new()
        {
            Skill = new SkillDefinition
            {
                Name = "skill-name",
                Description = "Description",
                Content = "Content",
                References = [.. references]
            }
        };
}
