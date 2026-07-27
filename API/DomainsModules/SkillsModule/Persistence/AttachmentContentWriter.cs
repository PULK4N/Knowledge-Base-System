using Microsoft.EntityFrameworkCore;
using SkillsModule.Application.Attachments;
using SkillsModule.Domain.Models;
using SkillsModule.Persistence.Models;

namespace SkillsModule.Persistence;

public sealed class AttachmentContentWriter(
    SkillsModuleDbContext dbContext
) : IAttachmentContentWriter
{
    public async Task Write(
        IEnumerable<(Attachment attachment, byte[] bytes)> attachments
    )
    {
        var contents = attachments
            .Select(
                attachment =>
                    new AttachmentContent
                    {
                        FileId = attachment.attachment.Id.Value,
                        Content = [.. attachment.bytes]
                    }
            )
            .ToArray();

        if (contents.Length == 0)
            return;

        await dbContext.AttachmentContents.AddRangeAsync(contents);
        await dbContext.SaveChangesAsync();
    }
}
