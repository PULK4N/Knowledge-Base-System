using ActionModule.Shared;
using ActionModule.Shared.Models;

namespace AttachmentsModule.Application.Queries;

public sealed class GetAttachmentContentQuery(
    IAttachmentContentStorage attachmentContentStorage
) : Query<AttachmentContentFile?>
{
    public Guid FileId { get; set; }

    protected override Task<AttachmentContentFile?> ExecuteInternal(
        Executor executor
    ) =>
        attachmentContentStorage.Get(FileId);
}
