using EventSourcing.Shared.Models;

namespace FeatureModule.Domain.Models;

public readonly record struct FeatureResearchDiscoveryId(Guid Value)
{
    public static FeatureResearchDiscoveryId New() =>
        new(DatabaseFriendlyGuidGenerator.NewGuid());

    public static FeatureResearchDiscoveryId FromDatabaseGuid(Guid value) =>
        new(value);
}

public enum FeatureResearchDiscoverySourceType
{
    Other,
    Code,
    Web,
    Mcp
}

public sealed class FeatureResearchDiscovery
{
    public FeatureResearchDiscoveryId Id { get; init; }

    public string Content { get; set; } = string.Empty;

    public FeatureResearchDiscoverySourceType SourceType { get; set; }

    /// <summary>
    /// Identifies where the discovery came from, such as a file path, URL, or MCP tool name.
    /// </summary>
    public string SourceReference { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
