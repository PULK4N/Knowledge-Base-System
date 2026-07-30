using ActionModule.Shared.Models;
using EventSourcing.Core;
using SkillsModule.Application.Attachments;
using SkillsModule.Domain.Events;
using SkillsModule.Domain.Models;

namespace SkillsModule.Application.Commands;

public sealed class DeleteSkillAttachmentCommand(
    StateMachineHandler stateMachineHandler,
    IAttachmentContentStorage attachmentContentStorage
) : ExistingSkillCommand(stateMachineHandler)
{
    public required FileId AttachmentId { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(AttachmentId.Value != Guid.Empty);

    protected override async Task<object> ExecuteInternal(
        Executor executor
    )
    {
        await attachmentContentStorage.Delete(AttachmentId);

        return await ExecuteEvent(
            executor,
            new SkillAttachmentDeletedV1(AttachmentId)
        );
    }
}
