using MemoryModule.Domain.Models;

namespace MemoryModule.Application.Models;

public sealed record MemoryAttachmentUpload(
    MemoryAttachment Attachment,
    byte[] Content
);
