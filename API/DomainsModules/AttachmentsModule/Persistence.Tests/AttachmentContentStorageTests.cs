using AttachmentsModule.Application;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AttachmentsModule.Persistence.Tests;

public sealed class AttachmentContentStorageTests
{
    [Fact]
    public async Task Saved_content_is_returned_with_its_metadata_until_deleted()
    {
        await using var dbContext = CreateDbContext();
        var storage = new AttachmentContentStorage(dbContext);
        var file = new AttachmentContentFile(
            Guid.NewGuid(),
            "screenshot.png",
            "image/png",
            [1, 2, 3]
        );

        await storage.Save(file);
        var stored = await storage.Get(file.FileId);
        await storage.Delete(file.FileId);

        Assert.NotNull(stored);
        Assert.Equal(file.Name, stored.Name);
        Assert.Equal(file.ContentType, stored.ContentType);
        Assert.Equal(file.Content, stored.Content);
        Assert.Null(await storage.Get(file.FileId));
    }

    [Fact]
    public async Task Missing_content_is_not_found_and_deleting_it_is_a_no_op()
    {
        await using var dbContext = CreateDbContext();
        var storage = new AttachmentContentStorage(dbContext);
        var fileId = Guid.NewGuid();

        await storage.Delete(fileId);

        Assert.Null(await storage.Get(fileId));
    }

    private static AttachmentsModuleDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<AttachmentsModuleDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options
        );
}
