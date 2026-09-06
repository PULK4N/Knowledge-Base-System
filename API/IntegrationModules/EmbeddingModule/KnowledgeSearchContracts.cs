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

public sealed record HybridKnowledgeSearchOptions
{
    public const int DefaultResultCount = 10;
    public const int DefaultCandidateCount = 50;
    public const int MaximumCandidateCount = 200;
    public const int DeduplicationOverfetchMultiplier = 4;

    public int ResultCount { get; init; } = DefaultResultCount;
    public int CandidateCount { get; init; } = DefaultCandidateCount;
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
        string query,
        int candidateCount,
        CancellationToken cancellationToken = default
    );

    Task<List<KnowledgeSearchCandidate>> SearchVector(
        ImmutableArray<float> embedding,
        int candidateCount,
        CancellationToken cancellationToken = default
    );
}

public interface IKnowledgeSearch
{
    Task<List<KnowledgeSearchResult>> Search(
        string query,
        HybridKnowledgeSearchOptions? options = null,
        CancellationToken cancellationToken = default
    );
}
