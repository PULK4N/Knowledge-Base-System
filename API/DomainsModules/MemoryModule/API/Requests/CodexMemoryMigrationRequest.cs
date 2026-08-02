using System.Text.Json.Serialization;

namespace MemoryModule.API.Requests;

public sealed record CodexMemoryMigrationRequest
{
    [JsonPropertyName("session_id")]
    public required Guid SessionId { get; init; }

    [JsonPropertyName("raw_memory")]
    public required string RawMemory { get; init; }

    [JsonPropertyName("rollout_summary")]
    public required string RolloutSummary { get; init; }

    [JsonPropertyName("source")]
    public required string Source { get; init; }
}
