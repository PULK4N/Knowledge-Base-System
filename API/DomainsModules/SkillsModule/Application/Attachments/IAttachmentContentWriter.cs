using SkillsModule.Domain.Models;

namespace SkillsModule.Application.Attachments;

public interface IAttachmentContentWriter
{
    Task Write(
        IEnumerable<(Attachment attachment, byte[] bytes)> attachments
    );
}
