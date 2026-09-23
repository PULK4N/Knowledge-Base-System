using System.Collections.Immutable;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using SkillsModule.Domain.Events;
using SkillsModule.Domain.Models;
using SkillsModule.Domain.Validators;

namespace SkillsModule.Domain.Tests;

public sealed class SkillMutationHistoryTests
{
    private static readonly AggregateId MemoryId = AggregateId.FromDatabaseGuid(
        Guid.Parse("10000000-0000-0000-0000-000000000000")
    );
    private static readonly Attachment Attachment = new()
    {
        Id = FileId.FromDatabaseGuid(Guid.Parse("11111111-1111-1111-1111-111111111111")),
        Name = "example.txt", Size = 1, FileType = "text/plain", Extension = "txt"
    };

    [Theory]
    [InlineData("create")]
    [InlineData("update")]
    [InlineData("delete")]
    [InlineData("reference-add")]
    [InlineData("reference-update")]
    [InlineData("reference-auto-load")]
    [InlineData("reference-delete")]
    [InlineData("attachment-add")]
    [InlineData("attachment-delete")]
    public void Mutation_records_required_memory_and_execution_metadata(string mutation)
    {
        var payload = CreatePayload(mutation);
        var state = CreateState(!mutation.EndsWith("-add"));

        var result = (SkillStateData)payload.EventData.Apply(state, payload.EventExecutionInfo);

        var history = Assert.Single(result.MemoryHistory);
        Assert.Equal(MemoryId, history.AggregateId);
        Assert.Equal(payload.EventExecutionInfo.EventName, history.EventName);
        Assert.Equal(payload.EventExecutionInfo.Timestamp, history.Timestamp);
    }

    [Theory]
    [InlineData("reference-add", false)]
    [InlineData("reference-add", true)]
    [InlineData("reference-update", false)]
    [InlineData("reference-update", true)]
    [InlineData("reference-auto-load", false)]
    [InlineData("reference-auto-load", true)]
    [InlineData("reference-delete", false)]
    [InlineData("reference-delete", true)]
    [InlineData("attachment-add", false)]
    [InlineData("attachment-add", true)]
    [InlineData("attachment-delete", false)]
    [InlineData("attachment-delete", true)]
    public void Current_version_validates_relationship_preconditions(string mutation, bool exists)
    {
        var state = CreateState(exists);
        var payload = CreatePayload(mutation);
        var result = mutation switch
        {
            "reference-add" => new SkillReferenceMustNotExistValidator().Validate(state, payload),
            "attachment-add" => new SkillAttachmentMustNotExistValidator().Validate(state, payload),
            "attachment-delete" => new SkillAttachmentMustExistValidator().Validate(state, payload),
            _ => new SkillReferenceMustExistValidator().Validate(state, payload)
        };

        Assert.Equal(mutation.EndsWith("-add") ? !exists : exists, result.Succeded);
    }

    private static SkillStateData CreateState(bool exists)
    {
        var state = new SkillStateData(AggregateId.FromDatabaseGuid(
            Guid.Parse("22222222-2222-2222-2222-222222222222")
        ));
        if (exists)
        {
            state.References["reference.md"] = new SkillReference2("old");
            state.Attachments[Attachment.Id] = Attachment;
        }
        return state;
    }

    private static EventPayload CreatePayload(string mutation)
    {
        IEvent eventData = mutation switch
        {
            "create" => new SkillCreatedV3("name", "description", "content", [],
                ImmutableDictionary<string, SkillReference2>.Empty, Guid.Empty, MemoryId),
            "update" => new SkillDetailsUpdatedV2("name", "description", "content", [], Guid.Empty, MemoryId),
            "delete" => new SkillDeletedV2(Guid.Empty, MemoryId),
            "reference-add" => new SkillReferenceAddedV3("reference.md", "new", true, Guid.Empty, MemoryId),
            "reference-update" => new SkillReferenceUpdatedV3("reference.md", "new", true, Guid.Empty, MemoryId),
            "reference-auto-load" => new SkillReferenceAutoLoadUpdatedV2("reference.md", true, Guid.Empty, MemoryId),
            "reference-delete" => new SkillReferenceDeletedV2("reference.md", Guid.Empty, MemoryId),
            "attachment-add" => new SkillAttachmentAddedV2(Attachment, Guid.Empty, MemoryId),
            "attachment-delete" => new SkillAttachmentDeletedV2(Attachment.Id, Guid.Empty, MemoryId),
            _ => throw new ArgumentOutOfRangeException(nameof(mutation))
        };
        return EventPayload.Create(
            EventExecutor.FromDatabaseGuid(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")),
            CreateState(false).Id,
            "skills-state-machine", eventData
        );
    }
}
