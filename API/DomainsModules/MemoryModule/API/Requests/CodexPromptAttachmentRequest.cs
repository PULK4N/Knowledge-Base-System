using System.Text.Json.Serialization;

namespace MemoryModule.API.Requests;

public sealed record CodexPromptAttachmentRequest
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("media_type")]
    public required string MediaType { get; init; }

    /// <summary>Base64-encoded file content.</summary>
    [JsonPropertyName("content")]
    public required byte[] Content { get; init; }

    [JsonPropertyName("placeholder")]
    public string? Placeholder { get; init; }
}
