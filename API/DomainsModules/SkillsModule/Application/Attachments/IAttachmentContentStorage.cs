using SkillsModule.Domain.Models;

namespace SkillsModule.Application.Attachments;

public interface IAttachmentContentStorage
{
    Task Save(Attachment attachment, byte[] bytes);
    Task Delete(FileId attachmentId);
}
