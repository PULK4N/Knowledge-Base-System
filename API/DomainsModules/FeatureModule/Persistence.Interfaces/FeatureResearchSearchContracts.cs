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

/// <summary>
/// Hybrid search results split into the plain top matches and one best match
/// per feature, so a single feature cannot fill the whole result set with its
/// own research discoveries.
/// </summary>
public sealed record FeatureResearchSearchResults(
    List<FeatureResearchSearchResult> TopMatches,
    List<FeatureResearchSearchResult> DistinctSources
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
    public const int DefaultSourceCandidateCount = 200;

    public int ResultCount { get; init; } = DefaultResultCount;
    public int CandidateCount { get; init; } = 50;
    public int SourceResultCount { get; init; } = DefaultResultCount;
    public int SourceCandidateCount { get; init; } = DefaultSourceCandidateCount;
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
        List<string> keywords,
        int candidateCount,
        CancellationToken cancellationToken = default
    );

    Task<List<FeatureResearchSearchCandidate>> SearchVector(
        ImmutableArray<float> embedding,
        int candidateCount,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Returns the best full-text match per feature, scanning
    /// <paramref name="candidateCount"/> ranked rows and keeping at most
    /// <paramref name="sourceCount"/> distinct features.
    /// </summary>
    Task<List<FeatureResearchSearchCandidate>> SearchTextBySource(
        List<string> keywords,
        int sourceCount,
        int candidateCount,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Returns the best vector match per feature, scanning
    /// <paramref name="candidateCount"/> nearest rows and keeping at most
    /// <paramref name="sourceCount"/> distinct features.
    /// </summary>
    Task<List<FeatureResearchSearchCandidate>> SearchVectorBySource(
        ImmutableArray<float> embedding,
        int sourceCount,
        int candidateCount,
        CancellationToken cancellationToken = default
    );
}

public interface IFeatureResearchSearch
{
    /// <param name="query">
    /// A meaningful sentence. Only the semantic vector leg reads it.
    /// </param>
    /// <param name="keywords">
    /// The words the full-text leg requires; a row matches when it contains
    /// all of them.
    /// </param>
    Task<List<FeatureResearchSearchResult>> Search(
        string query,
        List<string> keywords,
        HybridFeatureResearchSearchOptions? options = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Runs the ranked search and a second feature-grouped search over the same
    /// index, so callers also receive discoveries from distinct features
    /// instead of repeated chunks of one feature.
    /// </summary>
    Task<FeatureResearchSearchResults> SearchWithSources(
        string query,
        List<string> keywords,
        HybridFeatureResearchSearchOptions? options = null,
        CancellationToken cancellationToken = default
    );
}
