using FeatureModule.Domain;

namespace FeatureModule.Persistence;

internal static class FeatureResearchDiscoveryMarkdownCompiler
{
    public static List<FeatureResearchDiscoverySource> Compile(
        FeatureStateData feature
    ) =>
        feature.ResearchDiscoveries
            .Select(
                discovery => new FeatureResearchDiscoverySource(
                    discovery.Id.Value,
                    discovery.Title,
                    discovery.SourceType.ToString(),
                    discovery.SourceReference,
                    discovery.CreatedAt,
                    discovery.UpdatedAt,
                    $"# {discovery.Title}\n\n{discovery.Content}"
                )
            )
            .ToList();
}

internal sealed record FeatureResearchDiscoverySource(
    Guid ResearchDiscoveryId,
    string Title,
    string SourceType,
    string SourceReference,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string Markdown
);
