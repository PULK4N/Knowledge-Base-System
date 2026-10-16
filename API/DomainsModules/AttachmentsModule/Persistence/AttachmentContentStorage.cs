using AttachmentsModule.Application;
using AttachmentsModule.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace AttachmentsModule.Persistence;

public sealed class AttachmentContentStorage(
    AttachmentsModuleDbContext dbContext
) : IAttachmentContentStorage
{
    public async Task Save(AttachmentContentFile file)
    {
        var content = new AttachmentContent
        {
            FileId = file.FileId,
            Name = file.Name,
            ContentType = file.ContentType,
            Content = [.. file.Content]
        };

        await dbContext.AttachmentContents.AddAsync(content);
        await dbContext.SaveChangesAsync();
    }

    public async Task<AttachmentContentFile?> Get(Guid fileId)
    {
        var content = await dbContext.AttachmentContents
            .AsNoTracking()
            .SingleOrDefaultAsync(content => content.FileId == fileId);

        return content is null
            ? null
            : new AttachmentContentFile(
                content.FileId,
                content.Name,
                content.ContentType,
                content.Content
            );
    }

    public async Task Delete(Guid fileId)
    {
        var content = await dbContext.AttachmentContents.FindAsync(fileId);

        if (content is null)
            return;

        dbContext.AttachmentContents.Remove(content);
        await dbContext.SaveChangesAsync();
    }
}
