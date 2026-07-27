using System.Collections.Immutable;
using ActionModule.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Models;
using SkillsModule.Application.Attachments;
using SkillsModule.Domain.Events;
using SkillsModule.Domain.Models;

namespace SkillsModule.Application.Commands;

public sealed class AddSkillCommand(
    StateMachineHandler stateMachineHandler,
    IAttachmentContentWriter attachmentContentWriter
) : SkillCommand(stateMachineHandler)
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Content { get; init; }
    public List<string> Tags { get; init; } = [ ];
    public Dictionary<string, SkillReference> References { get; init; } =
        new(StringComparer.Ordinal);
    public IReadOnlyCollection<(Attachment attachment, byte[] bytes)> Attachments { get; init; } =
        [ ];

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var attachments = Attachments.ToList();

        await attachmentContentWriter.Write(attachments);

        return await ExecuteEvent(
            executor,
            AggregateId.New(),
            new SkillCreatedV1(
                Name,
                Description,
                Content,
                Tags.ToImmutableArray(),
                References.ToImmutableDictionary(StringComparer.Ordinal),
                attachments.ToImmutableDictionary(
                    attachment => attachment.attachment.Id,
                    attachment => attachment.attachment
                )
            )
        );
    }
}
