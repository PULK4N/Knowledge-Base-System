using ActionModule.API;
using ActionModule.Shared;
using AttachmentsModule.Application.Queries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using SharedModule.Constants;

namespace AttachmentsModule.API.Controllers;

[ApiController]
[Route(AttachmentRoutes.Base)]
public sealed class AttachmentsController(
    IExecutorProvider executorProvider
) : ActionController(executorProvider)
{
    /// <summary>
    /// Raster images are safe to show in place, so Markdown image links render
    /// them. Any other type, including SVG and HTML, could run script on this
    /// origin and is therefore only offered as a download.
    /// </summary>
    private static readonly HashSet<string> InlineContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "image/png",
            "image/jpeg",
            "image/gif",
            "image/webp",
            "image/bmp",
            "image/avif"
        };

    [HttpGet("{fileId:guid}")]
    public async Task<IActionResult> Get(
        Guid fileId,
        [FromServices] GetAttachmentContentQuery query
    )
    {
        query.FileId = fileId;

        var file = await Execute(query);
        if (file is null)
            return NotFound();

        var inline = InlineContentTypes.Contains(file.ContentType);
        var disposition = new ContentDispositionHeaderValue(
            inline ? "inline" : "attachment"
        );
        if (!string.IsNullOrWhiteSpace(file.Name))
            disposition.SetHttpFileName(file.Name);

        Response.Headers[HeaderNames.ContentDisposition] =
            disposition.ToString();
        Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";
        Response.Headers[HeaderNames.ContentSecurityPolicy] =
            "default-src 'none'; sandbox";

        return File(
            file.Content,
            inline ? file.ContentType : "application/octet-stream"
        );
    }
}
