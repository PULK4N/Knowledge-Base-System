using Microsoft.EntityFrameworkCore;
using SkillsModule.Application.Attachments;
using SkillsModule.Domain.Models;
using SkillsModule.Persistence.Models;

namespace SkillsModule.Persistence;

public sealed class AttachmentContentStorage(
    SkillsModuleDbContext dbContext
) : IAttachmentContentStorage
{
    public async Task Save(Attachment attachment, byte[] bytes)
    {
        var content = new AttachmentContent
        {
            FileId = attachment.Id.Value,
            Content = [.. bytes]
        };

        await dbContext.AttachmentContents.AddAsync(content);
        await dbContext.SaveChangesAsync();
    }

    public async Task Delete(FileId attachmentId)
    {
        var content = await dbContext.AttachmentContents.FindAsync(
            attachmentId.Value
        );

        if (content is null)
            return;

        dbContext.AttachmentContents.Remove(content);
        await dbContext.SaveChangesAsync();
    }
}
