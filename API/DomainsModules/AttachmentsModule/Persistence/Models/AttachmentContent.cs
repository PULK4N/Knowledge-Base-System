namespace AttachmentsModule.Persistence.Models;

public sealed class AttachmentContent
{
    public Guid FileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = [];
}
