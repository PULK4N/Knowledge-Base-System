using System.Collections.Immutable;
using EmbeddingModule;
using EventSourcing.Shared.Models;

namespace SkillsModule.Persistence.Interfaces;

public sealed record SkillSearchDocument(
    AggregateId SkillAggregateId,
    string SkillName,
    string SourcePath,
    int ChunkIndex,
    string Text,
    ImmutableArray<float> Embedding
);

public sealed record SkillSearchCandidate(
    AggregateId SkillAggregateId,
    string SkillName,
    string SourcePath,
    int ChunkIndex,
    string Text
);

public sealed record SkillSearchResult(
    SkillSearchCandidate Skill,
    double Score,
    int? TextRank,
    int? VectorRank
);

/// <summary>
/// Hybrid search results split into the plain top matches and one best match
/// per skill, so a single skill cannot fill the whole result set with its own
/// chunks.
/// </summary>
public sealed record SkillSearchResults(
    IReadOnlyList<SkillSearchResult> TopMatches,
    IReadOnlyList<SkillSearchResult> DistinctSources
);

public sealed record SkillSearchProjectionBatch(
    List<AggregateId> SkillAggregateIds,
    List<SkillSearchDocument> SkillDocuments,
    List<KnowledgeSearchDocument> KnowledgeDocuments
);

public interface ISkillSearchProjectionWriter
{
    Task Write(
        SkillSearchProjectionBatch batch,
        CancellationToken cancellationToken = default
    );
}

public sealed record HybridSkillSearchOptions
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

public interface ISkillSearchRepository
{
    Task Write(
        IReadOnlyCollection<AggregateId> skillAggregateIds,
        IReadOnlyCollection<SkillSearchDocument> documents,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<SkillSearchCandidate>> SearchText(
        List<string> keywords,
        int candidateCount,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<SkillSearchCandidate>> SearchVector(
        ImmutableArray<float> embedding,
        int candidateCount,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Returns the best full-text match per skill, scanning
    /// <paramref name="candidateCount"/> ranked rows and keeping at most
    /// <paramref name="sourceCount"/> distinct skills.
    /// </summary>
    Task<IReadOnlyList<SkillSearchCandidate>> SearchTextBySource(
        List<string> keywords,
        int sourceCount,
        int candidateCount,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Returns the best vector match per skill, scanning
    /// <paramref name="candidateCount"/> nearest rows and keeping at most
    /// <paramref name="sourceCount"/> distinct skills.
    /// </summary>
    Task<IReadOnlyList<SkillSearchCandidate>> SearchVectorBySource(
        ImmutableArray<float> embedding,
        int sourceCount,
        int candidateCount,
        CancellationToken cancellationToken = default
    );
}

public interface ISkillSearch
{
    /// <param name="query">
    /// A meaningful sentence. Only the semantic vector leg reads it.
    /// </param>
    /// <param name="keywords">
    /// The words the full-text leg requires; a row matches when it contains
    /// all of them.
    /// </param>
    Task<IReadOnlyList<SkillSearchResult>> Search(
        string query,
        List<string> keywords,
        HybridSkillSearchOptions? options = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Runs the ranked search and a second skill-grouped search over the same
    /// index, so callers also receive matches from distinct skills instead of
    /// repeated chunks of one skill.
    /// </summary>
    Task<SkillSearchResults> SearchWithSources(
        string query,
        List<string> keywords,
        HybridSkillSearchOptions? options = null,
        CancellationToken cancellationToken = default
    );
}
