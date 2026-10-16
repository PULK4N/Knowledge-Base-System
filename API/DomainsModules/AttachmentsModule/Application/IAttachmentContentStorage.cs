namespace AttachmentsModule.Application;

/// <summary>
/// Keeps attachment bytes outside the event stream. Every module stores its
/// attachments here, so one file ID resolves to its content no matter which
/// aggregate refers to it.
/// </summary>
public interface IAttachmentContentStorage
{
    Task Save(AttachmentContentFile file);
    Task<AttachmentContentFile?> Get(Guid fileId);
    Task Delete(Guid fileId);
}
