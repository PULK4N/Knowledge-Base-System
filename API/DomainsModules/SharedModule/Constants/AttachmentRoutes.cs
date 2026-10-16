namespace SharedModule.Constants;

/// <summary>
/// Lets modules write links to attachment content, for example images in
/// Markdown messages, without depending on the module that serves them.
/// </summary>
public static class AttachmentRoutes
{
    public const string Base = "api/attachments";

    public static string Content(Guid fileId) =>
        $"/{Base}/{fileId:D}";
}
