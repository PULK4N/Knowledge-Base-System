using FeatureModule.Persistence.Interfaces;

namespace FeatureModule.Application.DTOs;

public sealed record FeatureResearchSearchMatchDto(
    Guid FeatureId,
    string FeatureName,
    Guid ResearchDiscoveryId,
    string Title,
    string SourceType,
    string SourceReference,
    DateTime UpdatedAt,
    int ChunkIndex,
    string Text,
    double Score,
    int? TextRank,
    int? VectorRank
)
{
    public static FeatureResearchSearchMatchDto FromSearchResult(
        FeatureResearchSearchResult result
    )
    {
        var discovery = result.ResearchDiscovery;

        return new FeatureResearchSearchMatchDto(
            discovery.FeatureAggregateId.Value,
            discovery.FeatureName,
            discovery.ResearchDiscoveryId,
            discovery.Title,
            discovery.SourceType,
            discovery.SourceReference,
            discovery.UpdatedAt,
            discovery.ChunkIndex,
            discovery.Text,
            result.Score,
            result.TextRank,
            result.VectorRank
        );
    }
}
