using EventSourcing.Shared.Models;
using MemoryModule.Domain.Models;
using MemoryModule.Persistence.Interfaces;

namespace MemoryModule.Persistence;

public static class HybridMemoryRanker
{
    public const int RankConstant = 60;

    public static IReadOnlyList<MemorySearchResult> Fuse(
        IReadOnlyList<MemorySearchCandidate> textCandidates,
        IReadOnlyList<MemorySearchCandidate> vectorCandidates,
        HybridMemorySearchOptions options
    )
    {
        Validate(options);

        var results = new Dictionary<DocumentKey, MutableResult>();
        AddCandidates(
            results,
            textCandidates,
            options.TextWeight,
            isText: true
        );
        AddCandidates(
            results,
            vectorCandidates,
            options.VectorWeight,
            isText: false
        );

        return results.Values
            .OrderByDescending(result => result.Score)
            .ThenByDescending(result => result.Memory.PromptStartTimestamp)
            .ThenBy(result => result.Memory.MemoryAggregateId.Value)
            .ThenBy(result => result.Memory.PromptId.Value)
            .ThenBy(result => result.Memory.HookIndex)
            .ThenBy(result => result.Memory.ChunkIndex)
            .Take(options.ResultCount)
            .Select(
                result => new MemorySearchResult(
                    result.Memory,
                    result.Score,
                    result.TextRank,
                    result.VectorRank
                )
            )
            .ToList();
    }

    /// <summary>
    /// Fuses candidates that already hold one row per memory session, so the
    /// ranking keeps at most one match per session.
    /// </summary>
    public static IReadOnlyList<MemorySearchResult> FuseSources(
        IReadOnlyList<MemorySearchCandidate> textCandidates,
        IReadOnlyList<MemorySearchCandidate> vectorCandidates,
        HybridMemorySearchOptions options
    )
    {
        Validate(options);

        var results = new Dictionary<AggregateId, MutableResult>();
        AddSourceCandidates(
            results,
            textCandidates,
            options.TextWeight,
            isText: true
        );
        AddSourceCandidates(
            results,
            vectorCandidates,
            options.VectorWeight,
            isText: false
        );

        return results.Values
            .OrderByDescending(result => result.Score)
            .ThenByDescending(result => result.Memory.PromptStartTimestamp)
            .ThenBy(result => result.Memory.MemoryAggregateId.Value)
            .ThenBy(result => result.Memory.PromptId.Value)
            .ThenBy(result => result.Memory.HookIndex)
            .ThenBy(result => result.Memory.ChunkIndex)
            .Take(options.SourceResultCount)
            .Select(
                result => new MemorySearchResult(
                    result.Memory,
                    result.Score,
                    result.TextRank,
                    result.VectorRank
                )
            )
            .ToList();
    }

    public static void Validate(HybridMemorySearchOptions options)
    {
        if (options.ResultCount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(options.ResultCount)
            );
        if (options.CandidateCount < options.ResultCount)
            throw new ArgumentOutOfRangeException(
                nameof(options.CandidateCount),
                "Candidate count must be greater than or equal to result count."
            );
        if (options.SourceResultCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.SourceResultCount)
            );
        }
        if (options.SourceCandidateCount < options.SourceResultCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.SourceCandidateCount),
                "Source candidate count must be greater than or equal to source result count."
            );
        }
        if (options.TextWeight <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(options.TextWeight)
            );
        if (options.VectorWeight <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(options.VectorWeight)
            );
    }

    private static void AddCandidates(
        Dictionary<DocumentKey, MutableResult> results,
        IReadOnlyList<MemorySearchCandidate> candidates,
        double weight,
        bool isText
    )
    {
        for (var index = 0; index < candidates.Count; index++)
        {
            var candidate = candidates[index];
            var rank = index + 1;
            var key = DocumentKey.From(candidate);

            if (!results.TryGetValue(key, out var result))
            {
                result = new MutableResult(candidate);
                results.Add(key, result);
            }

            result.Score += weight / (RankConstant + rank);
            if (isText)
                result.TextRank = rank;
            else
                result.VectorRank = rank;
        }
    }

    private static void AddSourceCandidates(
        Dictionary<AggregateId, MutableResult> results,
        IReadOnlyList<MemorySearchCandidate> candidates,
        double weight,
        bool isText
    )
    {
        for (var index = 0; index < candidates.Count; index++)
        {
            var candidate = candidates[index];

            if (!results.TryGetValue(
                    candidate.MemoryAggregateId,
                    out var result
                ))
            {
                result = new MutableResult(candidate);
                results.Add(candidate.MemoryAggregateId, result);
            }

            // The source queries return one row per session, so a repeated
            // session within a single list must not score twice.
            if (isText
                ? result.TextRank is not null
                : result.VectorRank is not null)
            {
                continue;
            }

            var rank = index + 1;
            result.Score += weight / (RankConstant + rank);
            if (isText)
                result.TextRank = rank;
            else
                result.VectorRank = rank;
        }
    }

    private readonly record struct DocumentKey(
        AggregateId MemoryAggregateId,
        PromptId PromptId,
        int HookIndex,
        int ChunkIndex
    )
    {
        public static DocumentKey From(MemorySearchCandidate candidate) =>
            new(
                candidate.MemoryAggregateId,
                candidate.PromptId,
                candidate.HookIndex,
                candidate.ChunkIndex
            );
    }

    private sealed class MutableResult(MemorySearchCandidate memory)
    {
        public MemorySearchCandidate Memory { get; } = memory;
        public double Score { get; set; }
        public int? TextRank { get; set; }
        public int? VectorRank { get; set; }
    }
}
