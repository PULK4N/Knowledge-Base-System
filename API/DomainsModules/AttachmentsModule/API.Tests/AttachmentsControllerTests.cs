using ActionModule.Shared;
using ActionModule.Shared.Models;
using AttachmentsModule.API.Controllers;
using AttachmentsModule.Application;
using AttachmentsModule.Application.Queries;
using EventSourcing.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AttachmentsModule.API.Tests;

public sealed class AttachmentsControllerTests
{
    private static readonly Executor Executor =
        new()
        {
            Id = EventExecutor.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            )
        };

    [Theory]
    [InlineData("image/png", "inline", "image/png")]
    [InlineData("image/svg+xml", "attachment", "application/octet-stream")]
    [InlineData("text/html", "attachment", "application/octet-stream")]
    public async Task Get_shows_only_raster_images_inline(
        string storedContentType,
        string expectedDisposition,
        string expectedContentType
    )
    {
        var file = new AttachmentContentFile(
            Guid.NewGuid(),
            "file.bin",
            storedContentType,
            [1, 2, 3]
        );
        var controller = CreateController();

        var result = await controller.Get(
            file.FileId,
            new GetAttachmentContentQuery(new SingleFileStorage(file))
        );

        var content = Assert.IsType<FileContentResult>(result);
        Assert.Equal(file.Content, content.FileContents);
        Assert.Equal(expectedContentType, content.ContentType);
        Assert.StartsWith(
            expectedDisposition,
            controller.Response.Headers.ContentDisposition.ToString()
        );
        Assert.Equal(
            "nosniff",
            controller.Response.Headers.XContentTypeOptions.ToString()
        );
    }

    [Fact]
    public async Task Get_returns_not_found_for_an_unknown_file()
    {
        var result = await CreateController().Get(
            Guid.NewGuid(),
            new GetAttachmentContentQuery(new SingleFileStorage(null))
        );

        Assert.IsType<NotFoundResult>(result);
    }

    private static AttachmentsController CreateController() =>
        new(new StubExecutorProvider())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

    private sealed class StubExecutorProvider : IExecutorProvider
    {
        public Task<Executor> GetExecutor() => Task.FromResult(Executor);
    }

    private sealed class SingleFileStorage(AttachmentContentFile? file)
        : IAttachmentContentStorage
    {
        public Task Save(AttachmentContentFile file) =>
            throw new NotSupportedException();

        public Task<AttachmentContentFile?> Get(Guid fileId) =>
            Task.FromResult(file?.FileId == fileId ? file : null);

        public Task Delete(Guid fileId) =>
            throw new NotSupportedException();
    }
}
