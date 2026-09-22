using System.Collections.Immutable;
using System.Text.Json;
using EventSourcing.Shared.Models;

namespace EmbeddingModule;

public static class KnowledgeSearchOwnerTypes
{
    public const string Memory = "memory";
    public const string Skill = "skill";
    public const string Feature = "feature";
}

public static class KnowledgeSearchQueryLimits
{
    public const int MaximumLength = 1_000;
}

/// <summary>
/// Bounds for the full-text keyword list. Keywords are matched with AND, so a
/// long list narrows the text leg to nothing; the limits keep callers honest.
/// </summary>
public static class SearchKeywordLimits
{
    public const int MinimumCount = 1;
    public const int MaximumCount = 20;
    public const int MaximumKeywordLength = 100;

    public static bool AreValid(List<string>? keywords) =>
        keywords is not null
        && keywords.Count is >= MinimumCount and <= MaximumCount
        && keywords.All(
            keyword => !string.IsNullOrWhiteSpace(keyword)
                && keyword.Length <= MaximumKeywordLength
        );

    /// <summary>
    /// Joins the keywords for plainto_tsquery, which turns every word into an
    /// AND term and ignores full-text operator syntax typed by a caller.
    /// </summary>
    public static string ToTextQuery(List<string> keywords) =>
        string.Join(' ', keywords);
}

public static class KnowledgeSearchSourceTypes
{
    public const string MemoryPrompt = "memory_prompt";
    public const string MemorySummary = "memory_summary";
    public const string Skill = "skill";
    public const string Feature = "feature";
    public const string FeaturePlan = "feature_plan";
    public const string FeatureResearchDiscovery =
        "feature_research_discovery";
    public const string FeatureRecord = "feature_record";
}

public static class KnowledgeSearchMetadata
{
    public static JsonElement Create(Dictionary<string, object?> values) =>
        JsonSerializer.SerializeToElement(values);
}

public sealed record KnowledgeSearchDocument(
    string OwnerType,
    AggregateId OwnerAggregateId,
    string SourceType,
    string SourceKey,
    int ChunkIndex,
    DateTime? Timestamp,
    JsonElement Metadata,
    string SearchableMetadata,
    string Text,
    ImmutableArray<float> Embedding
);

public sealed record KnowledgeSearchCandidate(
    int Id,
    string OwnerType,
    AggregateId OwnerAggregateId,
    string SourceType,
    string SourceKey,
    int ChunkIndex,
    DateTime? Timestamp,
    JsonElement Metadata,
    string Text
);

public sealed record KnowledgeSearchResult(
    KnowledgeSearchCandidate Document,
    double Score,
    int? TextRank,
    int? VectorRank
);

/// <summary>
/// Hybrid search results split into the plain top matches and one best match
/// per owning source, so a single skill, feature, or memory cannot fill the
/// whole result set on its own.
/// </summary>
public sealed record KnowledgeSearchResults(
    List<KnowledgeSearchResult> TopMatches,
    List<KnowledgeSearchResult> DistinctSources
);

public sealed record HybridKnowledgeSearchOptions
{
    public const int DefaultResultCount = 10;
    public const int DefaultCandidateCount = 50;
    public const int MaximumCandidateCount = 200;
    public const int DeduplicationOverfetchMultiplier = 4;
    public const int DefaultSourceCandidateCount = 200;

    public int ResultCount { get; init; } = DefaultResultCount;
    public int CandidateCount { get; init; } = DefaultCandidateCount;
    public int SourceResultCount { get; init; } = DefaultResultCount;
    public int SourceCandidateCount { get; init; } = DefaultSourceCandidateCount;
    public double TextWeight { get; init; } = 1;
    public double VectorWeight { get; init; } = 1;
}

public interface IKnowledgeSearchRepository
{
    Task Write(
        string ownerType,
        List<AggregateId> ownerAggregateIds,
        List<KnowledgeSearchDocument> documents,
        CancellationToken cancellationToken = default
    );

    Task<List<KnowledgeSearchCandidate>> SearchText(
        List<string> keywords,
        int candidateCount,
        CancellationToken cancellationToken = default
    );

    Task<List<KnowledgeSearchCandidate>> SearchVector(
        ImmutableArray<float> embedding,
        int candidateCount,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Returns the best full-text match per owning source, scanning
    /// <paramref name="candidateCount"/> ranked rows and keeping at most
    /// <paramref name="sourceCount"/> distinct owners.
    /// </summary>
    Task<List<KnowledgeSearchCandidate>> SearchTextBySource(
        List<string> keywords,
        int sourceCount,
        int candidateCount,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Returns the best vector match per owning source, scanning
    /// <paramref name="candidateCount"/> nearest rows and keeping at most
    /// <paramref name="sourceCount"/> distinct owners.
    /// </summary>
    Task<List<KnowledgeSearchCandidate>> SearchVectorBySource(
        ImmutableArray<float> embedding,
        int sourceCount,
        int candidateCount,
        CancellationToken cancellationToken = default
    );
}

public interface IKnowledgeSearch
{
    /// <param name="query">
    /// A meaningful sentence. Only the semantic vector leg reads it.
    /// </param>
    /// <param name="keywords">
    /// The words the full-text leg requires; a row matches when it contains
    /// all of them.
    /// </param>
    Task<List<KnowledgeSearchResult>> Search(
        string query,
        List<string> keywords,
        HybridKnowledgeSearchOptions? options = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Runs the ranked search and a second source-grouped search over the same
    /// index, so callers also receive matches from distinct skills, features,
    /// and memories instead of repeated chunks of one source.
    /// </summary>
    Task<KnowledgeSearchResults> SearchWithSources(
        string query,
        List<string> keywords,
        HybridKnowledgeSearchOptions? options = null,
        CancellationToken cancellationToken = default
    );
}
