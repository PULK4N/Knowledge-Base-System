using EventSourcing.Shared.Models;
using SkillsModule.Domain.Events;
using SkillsModule.Domain.Models;
using SkillsModule.Domain.Validators;

namespace SkillsModule.Domain.Tests;

public sealed class SkillReferenceValidatorTests
{
    [Fact]
    public void MustNotExist_FailsForDuplicateRelativePath()
    {
        var payload = CreatePayload(
            new SkillReferenceAddedV1
            {
                RelativePath = "references/example.md",
                Content = "Duplicate content"
            }
        );
        var state = CreateState("references/example.md");
        var validator = new SkillReferenceMustNotExistValidator();

        var result = validator.Validate(state, payload);

        Assert.False(result.Succeded);
        Assert.Equal(
            nameof(SkillReferenceMustNotExistValidator),
            result.ValidatorName
        );
        Assert.Contains("already exists", result.FailureReason);
    }

    [Fact]
    public void MustNotExist_SucceedsForNewRelativePath()
    {
        var payload = CreatePayload(
            new SkillReferenceAddedV1
            {
                RelativePath = "references/new.md",
                Content = "Content"
            }
        );
        var validator = new SkillReferenceMustNotExistValidator();

        var result = validator.Validate(CreateState(), payload);

        Assert.True(result.Succeded);
        Assert.Null(result.FailureReason);
    }

    [Fact]
    public void MustExist_FailsForUnknownRelativePath()
    {
        var payload = CreatePayload(
            new SkillReferenceDeletedV1
            {
                RelativePath = "references/missing.md"
            }
        );
        var validator = new SkillReferenceMustExistValidator();

        var result = validator.Validate(CreateState(), payload);

        Assert.False(result.Succeded);
        Assert.Equal(
            nameof(SkillReferenceMustExistValidator),
            result.ValidatorName
        );
        Assert.Contains("does not exist", result.FailureReason);
    }

    [Fact]
    public void MustExist_SucceedsForKnownRelativePath()
    {
        var payload = CreatePayload(
            new SkillReferenceUpdatedV1
            {
                RelativePath = "references/example.md",
                Content = "Updated content"
            }
        );
        var validator = new SkillReferenceMustExistValidator();

        var result = validator.Validate(
            CreateState("references/example.md"),
            payload
        );

        Assert.True(result.Succeded);
        Assert.Null(result.FailureReason);
    }

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

    private static SkillStateData CreateState(params string[] relativePaths) =>
        new()
        {
            Name = "skill-name",
            References = relativePaths
                .ToDictionary(
                    relativePath => relativePath,
                    _ => new SkillReference("Content"),
                    StringComparer.Ordinal
                )
        };
}
