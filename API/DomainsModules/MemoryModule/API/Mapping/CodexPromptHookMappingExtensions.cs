using System.Text.Json;
using EventSourcing.Shared.Models;
using MemoryModule.API.Requests;
using MemoryModule.Application.Commands;
using MemoryModule.Application.Models;
using MemoryModule.Domain.Models;

namespace MemoryModule.API.Mapping;

public static class CodexPromptHookMappingExtensions
{
    /// <summary>
    /// Accepts the hook input as the whole body, or wrapped as
    /// <c>{ "hook": ..., "attachments": [...] }</c> when the prompt carries
    /// pasted images. Only the hook input is kept as the event payload, so the
    /// attachment content never reaches the event stream.
    /// </summary>
    public static void MapTo(
        this JsonElement body,
        RecordCodexPromptHookCommand command
    )
    {
        var payload = body.TryGetProperty("hook", out var hook) ? hook : body;

        command.ThreadId = new ThreadId(
            payload.GetProperty("session_id").GetGuid()
        );
        command.PromptId = new PromptId(
            payload.GetProperty("turn_id").GetGuid()
        );
        command.HookEventName = payload
            .GetProperty("hook_event_name")
            .GetString()!;
        command.Payload = payload;
        command.Attachments = body.TryGetProperty("attachments", out var attachments)
            ? attachments
                .Deserialize<List<CodexPromptAttachmentRequest>>()!
                .Select(ToUpload)
                .ToList()
            : [];
    }

    private static MemoryAttachmentUpload ToUpload(
        CodexPromptAttachmentRequest request
    ) =>
        new(
            new MemoryAttachment
            {
                Id = new AttachmentId(DatabaseFriendlyGuidGenerator.NewGuid()),
                Name = request.Name,
                Size = request.Content.LongLength,
                FileType = request.MediaType,
                Extension = Path.GetExtension(request.Name).TrimStart('.'),
                UserMessageIndex = request.UserMessageIndex,
                Placeholder = string.IsNullOrWhiteSpace(request.Placeholder)
                    ? null
                    : request.Placeholder
            },
            request.Content
        );
}
