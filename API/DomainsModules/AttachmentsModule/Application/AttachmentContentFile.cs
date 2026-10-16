namespace AttachmentsModule.Application;

public sealed record AttachmentContentFile(
    Guid FileId,
    string Name,
    string ContentType,
    byte[] Content
);
