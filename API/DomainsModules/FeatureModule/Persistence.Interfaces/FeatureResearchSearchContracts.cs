using System.Collections.Immutable;
using EmbeddingModule;
using EventSourcing.Shared.Models;

namespace FeatureModule.Persistence.Interfaces;

public sealed record FeatureResearchSearchDocument(
    AggregateId FeatureAggregateId,
    string FeatureName,
    Guid ResearchDiscoveryId,
    string Title,
    string SourceType,
    string SourceReference,
    DateTime UpdatedAt,
    int ChunkIndex,
    string Text,
    ImmutableArray<float> Embedding
);

public sealed record FeatureResearchSearchCandidate(
    AggregateId FeatureAggregateId,
    string FeatureName,
    Guid ResearchDiscoveryId,
    string Title,
    string SourceType,
    string SourceReference,
    DateTime UpdatedAt,
    int ChunkIndex,
    string Text
);

public sealed record FeatureResearchSearchResult(
    FeatureResearchSearchCandidate ResearchDiscovery,
    double Score,
    int? TextRank,
    int? VectorRank
);

public sealed record FeatureSearchProjectionBatch(
    List<AggregateId> FeatureAggregateIds,
    List<FeatureResearchSearchDocument> ResearchDocuments,
    List<KnowledgeSearchDocument> KnowledgeDocuments
);

public interface IFeatureSearchProjectionWriter
{
    Task Write(
        FeatureSearchProjectionBatch batch,
        CancellationToken cancellationToken = default
    );
}

public sealed record HybridFeatureResearchSearchOptions
{
    public const int DefaultResultCount = 5;

    public int ResultCount { get; init; } = DefaultResultCount;
    public int CandidateCount { get; init; } = 50;
    public double TextWeight { get; init; } = 1;
    public double VectorWeight { get; init; } = 1;
}

public interface IFeatureResearchSearchRepository
{
    Task Write(
        List<AggregateId> featureAggregateIds,
        List<FeatureResearchSearchDocument> documents,
        CancellationToken cancellationToken = default
    );

    Task<List<FeatureResearchSearchCandidate>> SearchText(
        string query,
        int candidateCount,
        CancellationToken cancellationToken = default
    );

    Task<List<FeatureResearchSearchCandidate>> SearchVector(
        ImmutableArray<float> embedding,
        int candidateCount,
        CancellationToken cancellationToken = default
    );
}

public interface IFeatureResearchSearch
{
    Task<List<FeatureResearchSearchResult>> Search(
        string query,
        HybridFeatureResearchSearchOptions? options = null,
        CancellationToken cancellationToken = default
    );
}
