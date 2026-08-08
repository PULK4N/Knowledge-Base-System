using System.Collections.Immutable;
using EventSourcing.Shared.Models;
using MemoryModule.Domain.Models;

namespace MemoryModule.Persistence.Interfaces;

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

public sealed record HybridMemorySearchOptions
{
    public int ResultCount { get; init; } = 10;
    public int CandidateCount { get; init; } = 50;
    public double TextWeight { get; init; } = 1;
    public double VectorWeight { get; init; } = 1;
}

public interface IMemorySearchRepository
{
    Task Replace(
        IReadOnlyCollection<AggregateId> memoryAggregateIds,
        IReadOnlyCollection<MemorySearchDocument> documents,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<MemorySearchCandidate>> SearchText(
        string query,
        int candidateCount,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<MemorySearchCandidate>> SearchVector(
        ImmutableArray<float> embedding,
        int candidateCount,
        CancellationToken cancellationToken = default
    );
}

public interface IMemoryEmbeddingGenerator
{
    Task<IReadOnlyList<ImmutableArray<float>>> Generate(
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken = default
    );
}

public interface IMemorySearch
{
    Task<IReadOnlyList<MemorySearchResult>> Search(
        string query,
        HybridMemorySearchOptions? options = null,
        CancellationToken cancellationToken = default
    );
}
