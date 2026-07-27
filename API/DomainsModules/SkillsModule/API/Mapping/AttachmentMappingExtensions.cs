using Microsoft.AspNetCore.Http;
using SkillsModule.Domain.Models;

namespace SkillsModule.API.Mapping;

public static class AttachmentMappingExtensions
{
    public static async Task<
        IEnumerable<(Attachment attachment, byte[] bytes)>
    > MapToAttachments(this IEnumerable<IFormFile>? files)
    {
        if (files is null)
            return [];

        var tasks = files.Select(
            async file =>
            {
                var attachment = new Attachment
                {
                    Id = FileId.New(),
                    Name = file.FileName,
                    Size = file.Length,
                    FileType = file.ContentType,
                    Extension = Path
                        .GetExtension(file.FileName)
                        .TrimStart('.')
                };

                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);

                return (attachment, stream.ToArray());
            }
        );

        return await Task.WhenAll(tasks);
    }
}
