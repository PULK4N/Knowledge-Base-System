using EventSourcing.Shared.Models;
using FeatureModule.Persistence.Interfaces;

namespace FeatureModule.Persistence;

public static class HybridFeatureRecordRanker
{
    public const int RankConstant = 60;

    public static List<FeatureRecordSearchResult> Fuse(
        List<FeatureRecordSearchCandidate> textCandidates,
        List<FeatureRecordSearchCandidate> vectorCandidates,
        HybridFeatureRecordSearchOptions options
    )
    {
        Validate(options);

        var results = new Dictionary<DocumentKey, MutableResult>();
        AddCandidates(results, textCandidates, options.TextWeight, true);
        AddCandidates(results, vectorCandidates, options.VectorWeight, false);

        return results.Values
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Record.FeatureName, StringComparer.Ordinal)
            .ThenBy(result => result.Record.Title, StringComparer.Ordinal)
            .ThenBy(result => result.Record.ChunkIndex)
            .DistinctBy(result => SourceKey.From(result.Record))
            .Take(options.ResultCount)
            .Select(
                result => new FeatureRecordSearchResult(
                    result.Record,
                    result.Score,
                    result.TextRank,
                    result.VectorRank
                )
            )
            .ToList();
    }

    /// <summary>
    /// Fuses candidates that already hold one row per feature, so the ranking
    /// keeps at most one discovery per feature.
    /// </summary>
    public static List<FeatureRecordSearchResult> FuseSources(
        List<FeatureRecordSearchCandidate> textCandidates,
        List<FeatureRecordSearchCandidate> vectorCandidates,
        HybridFeatureRecordSearchOptions options
    )
    {
        Validate(options);

        var results = new Dictionary<AggregateId, MutableResult>();
        AddSourceCandidates(results, textCandidates, options.TextWeight, true);
        AddSourceCandidates(
            results,
            vectorCandidates,
            options.VectorWeight,
            false
        );

        return results.Values
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Record.FeatureName, StringComparer.Ordinal)
            .ThenBy(result => result.Record.Title, StringComparer.Ordinal)
            .ThenBy(result => result.Record.ChunkIndex)
            .Take(options.SourceResultCount)
            .Select(
                result => new FeatureRecordSearchResult(
                    result.Record,
                    result.Score,
                    result.TextRank,
                    result.VectorRank
                )
            )
            .ToList();
    }

    public static void Validate(HybridFeatureRecordSearchOptions options)
    {
        if (options.ResultCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.ResultCount));
        if (options.CandidateCount < options.ResultCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.CandidateCount),
                "Candidate count must be greater than or equal to result count."
            );
        }
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
            throw new ArgumentOutOfRangeException(nameof(options.TextWeight));
        if (options.VectorWeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.VectorWeight));
    }

    private static void AddCandidates(
        Dictionary<DocumentKey, MutableResult> results,
        List<FeatureRecordSearchCandidate> candidates,
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
        List<FeatureRecordSearchCandidate> candidates,
        double weight,
        bool isText
    )
    {
        for (var index = 0; index < candidates.Count; index++)
        {
            var candidate = candidates[index];

            if (!results.TryGetValue(
                    candidate.FeatureAggregateId,
                    out var result
                ))
            {
                result = new MutableResult(candidate);
                results.Add(candidate.FeatureAggregateId, result);
            }

            // The source queries return one row per feature, so a repeated
            // feature within a single list must not score twice.
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
        AggregateId FeatureAggregateId,
        Guid RecordId,
        int ChunkIndex
    )
    {
        public static DocumentKey From(
            FeatureRecordSearchCandidate candidate
        ) =>
            new(
                candidate.FeatureAggregateId,
                candidate.RecordId,
                candidate.ChunkIndex
            );
    }

    private readonly record struct SourceKey(
        AggregateId FeatureAggregateId,
        Guid RecordId
    )
    {
        public static SourceKey From(
            FeatureRecordSearchCandidate candidate
        ) =>
            new(
                candidate.FeatureAggregateId,
                candidate.RecordId
            );
    }

    private sealed class MutableResult(
        FeatureRecordSearchCandidate record
    )
    {
        public FeatureRecordSearchCandidate Record { get; } = record;
        public double Score { get; set; }
        public int? TextRank { get; set; }
        public int? VectorRank { get; set; }
    }
}
