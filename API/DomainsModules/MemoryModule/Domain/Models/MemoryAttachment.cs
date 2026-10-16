namespace MemoryModule.Domain.Models;

public sealed record MemoryAttachment
{
    public required AttachmentId Id { get; init; }
    public required string Name { get; init; }
    public required long Size { get; init; }
    public required string FileType { get; init; }
    public required string Extension { get; init; }

    /// <summary>
    /// The text in the prompt that stands for this attachment, such as
    /// "[Image #2]" for an image pasted into Claude Code. Readers replace it with
    /// a link to the content; null when the prompt has no marker for it.
    /// </summary>
    public string? Placeholder { get; init; }
}
