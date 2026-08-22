using System.Collections.Immutable;
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

public sealed record HybridSkillSearchOptions
{
    public const int DefaultResultCount = 5;

    public int ResultCount { get; init; } = DefaultResultCount;
    public int CandidateCount { get; init; } = 50;
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
        string query,
        int candidateCount,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<SkillSearchCandidate>> SearchVector(
        ImmutableArray<float> embedding,
        int candidateCount,
        CancellationToken cancellationToken = default
    );
}

public interface ISkillSearch
{
    Task<IReadOnlyList<SkillSearchResult>> Search(
        string query,
        HybridSkillSearchOptions? options = null,
        CancellationToken cancellationToken = default
    );
}
