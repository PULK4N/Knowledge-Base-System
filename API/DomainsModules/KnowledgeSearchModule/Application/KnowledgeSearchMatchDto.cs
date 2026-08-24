using System.Text.Json;
using EmbeddingModule;

namespace KnowledgeSearchModule.Application;

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
