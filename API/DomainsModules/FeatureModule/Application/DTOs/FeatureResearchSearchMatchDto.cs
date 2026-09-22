using FeatureModule.Persistence.Interfaces;

namespace FeatureModule.Application.DTOs;

/// <summary>
/// Search response carrying the ranked top matches and, separately, the best
/// discovery per distinct feature, so repeated chunks of one feature cannot
/// hide the other features that matched.
/// </summary>
public sealed record FeatureResearchSearchResultsDto(
    List<FeatureResearchSearchMatchDto> TopMatches,
    List<FeatureResearchSearchMatchDto> DistinctSources
)
{
    public static FeatureResearchSearchResultsDto FromSearchResults(
        FeatureResearchSearchResults results
    ) =>
        new(
            results.TopMatches
                .Select(FeatureResearchSearchMatchDto.FromSearchResult)
                .ToList(),
            results.DistinctSources
                .Select(FeatureResearchSearchMatchDto.FromSearchResult)
                .ToList()
        );
}

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
