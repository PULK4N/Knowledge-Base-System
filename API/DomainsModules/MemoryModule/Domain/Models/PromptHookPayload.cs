using System.Text.Json;

namespace MemoryModule.Domain.Models;

/// <summary>
/// Claude and Codex hook payloads carry the conversation text in "prompt" for a
/// user turn and in "last_assistant_message" for an assistant turn. Everything
/// else in the payload is transport metadata such as session identifiers and
/// paths, which is noise for embedding and retrieval.
/// </summary>
public static class PromptHookPayload
{
    public const string PromptPropertyName = "prompt";
    public const string AssistantMessagePropertyName = "last_assistant_message";

    /// <summary>
    /// Returns the conversation text of the hook, or null when the payload
    /// carries neither message property so callers can fall back to the whole
    /// payload.
    /// </summary>
    public static string? FindMessage(JsonElement payload) =>
        ReadText(payload, PromptPropertyName)
        ?? ReadText(payload, AssistantMessagePropertyName);

    private static string? ReadText(JsonElement payload, string propertyName)
    {
        if (
            payload.ValueKind != JsonValueKind.Object
            || !payload.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.String
        )
            return null;

        var text = property.GetString();

        return string.IsNullOrWhiteSpace(text) ? null : text;
    }
}
