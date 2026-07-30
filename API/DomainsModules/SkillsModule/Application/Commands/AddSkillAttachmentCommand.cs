using ActionModule.Models;
using EventSourcing.Core;
using SkillsModule.Application.Attachments;
using SkillsModule.Domain.Events;
using SkillsModule.Domain.Models;

namespace SkillsModule.Application.Commands;

public sealed class AddSkillAttachmentCommand(
    StateMachineHandler stateMachineHandler,
    IAttachmentContentStorage attachmentContentStorage
) : ExistingSkillCommand(stateMachineHandler)
{
    public required Attachment Attachment { get; set; }
    public required byte[] Bytes { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            Attachment.Id.Value != Guid.Empty
            && Attachment.Size == Bytes.LongLength
        );

    protected override async Task<object> ExecuteInternal(
        Executor executor
    )
    {
        await attachmentContentStorage.Save(Attachment, Bytes);

        return await ExecuteEvent(
            executor,
            new SkillAttachmentAddedV1(Attachment)
        );
    }
}
