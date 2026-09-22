using System.Collections.Immutable;
using EmbeddingModule;
using EventSourcing.Shared.Models;
using MemoryModule.Domain.Models;

namespace MemoryModule.Persistence.Interfaces;

public static class MemorySearchDocumentSources
{
    public const string ChatSummary = "chat_summary";
}

public sealed record MemorySearchDocument(
    AggregateId MemoryAggregateId,
    ThreadId ThreadId,
    PromptId PromptId,
    int HookIndex,
    int ChunkIndex,
    DateTime PromptStartTimestamp,
    string HookEventName,
    string Text,
    ImmutableArray<float> Embedding
);

public sealed record MemorySearchCandidate(
    AggregateId MemoryAggregateId,
    ThreadId ThreadId,
    PromptId PromptId,
    int HookIndex,
    int ChunkIndex,
    DateTime PromptStartTimestamp,
    string HookEventName,
    string Text
);

public sealed record MemorySearchResult(
    MemorySearchCandidate Memory,
    double Score,
    int? TextRank,
    int? VectorRank
);

/// <summary>
/// Hybrid search results split into the plain top matches and one best match
/// per memory session, so a single session cannot fill the whole result set
/// with its own chunks.
/// </summary>
public sealed record MemorySearchResults(
    IReadOnlyList<MemorySearchResult> TopMatches,
    IReadOnlyList<MemorySearchResult> DistinctSources
);

public sealed record MemorySearchProjectionBatch(
    List<AggregateId> MemoryAggregateIds,
    List<MemorySearchDocument> MemoryDocuments,
    List<KnowledgeSearchDocument> KnowledgeDocuments
);

public interface IMemorySearchProjectionWriter
{
    Task Write(
        MemorySearchProjectionBatch batch,
        CancellationToken cancellationToken = default
    );
}

public sealed record HybridMemorySearchOptions
{
    public const int DefaultSourceResultCount = 10;
    public const int DefaultSourceCandidateCount = 200;

    public int ResultCount { get; init; } = 10;
    public int CandidateCount { get; init; } = 50;
    public int SourceResultCount { get; init; } = DefaultSourceResultCount;
    public int SourceCandidateCount { get; init; } = DefaultSourceCandidateCount;
    public double TextWeight { get; init; } = 1;
    public double VectorWeight { get; init; } = 1;
}

public interface IMemorySearchRepository
{
    Task Write(
        IReadOnlyCollection<AggregateId> memoryAggregateIds,
        IReadOnlyCollection<MemorySearchDocument> documents,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<MemorySearchCandidate>> SearchText(
        List<string> keywords,
        int candidateCount,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<MemorySearchCandidate>> SearchVector(
        ImmutableArray<float> embedding,
        int candidateCount,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Returns the best full-text match per memory session, scanning
    /// <paramref name="candidateCount"/> ranked rows and keeping at most
    /// <paramref name="sourceCount"/> distinct sessions.
    /// </summary>
    Task<IReadOnlyList<MemorySearchCandidate>> SearchTextBySource(
        List<string> keywords,
        int sourceCount,
        int candidateCount,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Returns the best vector match per memory session, scanning
    /// <paramref name="candidateCount"/> nearest rows and keeping at most
    /// <paramref name="sourceCount"/> distinct sessions.
    /// </summary>
    Task<IReadOnlyList<MemorySearchCandidate>> SearchVectorBySource(
        ImmutableArray<float> embedding,
        int sourceCount,
        int candidateCount,
        CancellationToken cancellationToken = default
    );
}

public interface IMemorySearch
{
    /// <param name="query">
    /// A meaningful sentence. Only the semantic vector leg reads it.
    /// </param>
    /// <param name="keywords">
    /// The words the full-text leg requires; a row matches when it contains
    /// all of them.
    /// </param>
    Task<IReadOnlyList<MemorySearchResult>> Search(
        string query,
        List<string> keywords,
        HybridMemorySearchOptions? options = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Runs the ranked search and a second session-grouped search over the same
    /// index, so callers also receive matches from distinct memory sessions
    /// instead of repeated chunks of one session.
    /// </summary>
    Task<MemorySearchResults> SearchWithSources(
        string query,
        List<string> keywords,
        HybridMemorySearchOptions? options = null,
        CancellationToken cancellationToken = default
    );
}
