using FeatureModule.Persistence.Interfaces;

namespace FeatureModule.Application.DTOs;

/// <summary>
/// Search response carrying the ranked top matches and, separately, the best
/// record per distinct feature, so repeated chunks of one feature cannot
/// hide the other features that matched.
/// </summary>
public sealed record FeatureRecordSearchResultsDto(
    List<FeatureRecordSearchMatchDto> TopMatches,
    List<FeatureRecordSearchMatchDto> DistinctSources
)
{
    public static FeatureRecordSearchResultsDto FromSearchResults(
        FeatureRecordSearchResults results
    ) =>
        new(
            results.TopMatches
                .Select(FeatureRecordSearchMatchDto.FromSearchResult)
                .ToList(),
            results.DistinctSources
                .Select(FeatureRecordSearchMatchDto.FromSearchResult)
                .ToList()
        );
}

public sealed record FeatureRecordSearchMatchDto(
    Guid FeatureId,
    string FeatureName,
    Guid RecordId,
    string RecordType,
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
    public static FeatureRecordSearchMatchDto FromSearchResult(
        FeatureRecordSearchResult result
    )
    {
        var record = result.Record;

        return new FeatureRecordSearchMatchDto(
            record.FeatureAggregateId.Value,
            record.FeatureName,
            record.RecordId,
            record.RecordType,
            record.Title,
            record.SourceType,
            record.SourceReference,
            record.UpdatedAt,
            record.ChunkIndex,
            record.Text,
            result.Score,
            result.TextRank,
            result.VectorRank
        );
    }
}
