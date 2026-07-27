namespace SkillsModule.Persistence.Models;

public sealed class AttachmentContent
{
    public Guid FileId { get; set; }
    public byte[] Content { get; set; } = [];
}
