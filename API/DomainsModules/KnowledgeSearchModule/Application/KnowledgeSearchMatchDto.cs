using System.Text.Json;
using EmbeddingModule;

namespace KnowledgeSearchModule.Application;

/// <summary>
/// Search response carrying the ranked top matches and, separately, the best
/// match per distinct source, so repeated chunks of one skill, feature, or
/// memory cannot hide the other sources that matched.
/// </summary>
public sealed record KnowledgeSearchResultsDto(
    List<KnowledgeSearchMatchDto> TopMatches,
    List<KnowledgeSearchMatchDto> DistinctSources
)
{
    public static KnowledgeSearchResultsDto FromResults(
        KnowledgeSearchResults results
    ) =>
        new(
            results.TopMatches.Select(KnowledgeSearchMatchDto.FromResult).ToList(),
            results.DistinctSources
                .Select(KnowledgeSearchMatchDto.FromResult)
                .ToList()
        );
}

public sealed record KnowledgeSearchMatchDto(
    string OwnerType,
    Guid OwnerId,
    string SourceType,
    string SourceKey,
    int ChunkIndex,
    DateTime? Timestamp,
    JsonElement Metadata,
    string Text,
    double Score,
    int? TextRank,
    int? VectorRank
)
{
    public static KnowledgeSearchMatchDto FromResult(
        KnowledgeSearchResult result
    )
    {
        var document = result.Document;

        return new KnowledgeSearchMatchDto(
            document.OwnerType,
            document.OwnerAggregateId.Value,
            document.SourceType,
            document.SourceKey,
            document.ChunkIndex,
            document.Timestamp,
            document.Metadata,
            document.Text,
            result.Score,
            result.TextRank,
            result.VectorRank
        );
    }
}
