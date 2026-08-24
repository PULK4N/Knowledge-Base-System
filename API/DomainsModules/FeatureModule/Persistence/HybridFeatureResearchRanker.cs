using EventSourcing.Shared.Models;
using FeatureModule.Persistence.Interfaces;

namespace FeatureModule.Persistence;

public static class HybridFeatureResearchRanker
{
    public const int RankConstant = 60;

    public static List<FeatureResearchSearchResult> Fuse(
        List<FeatureResearchSearchCandidate> textCandidates,
        List<FeatureResearchSearchCandidate> vectorCandidates,
        HybridFeatureResearchSearchOptions options
    )
    {
        Validate(options);

        var results = new Dictionary<DocumentKey, MutableResult>();
        AddCandidates(results, textCandidates, options.TextWeight, true);
        AddCandidates(results, vectorCandidates, options.VectorWeight, false);

        return results.Values
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Discovery.FeatureName, StringComparer.Ordinal)
            .ThenBy(result => result.Discovery.Title, StringComparer.Ordinal)
            .ThenBy(result => result.Discovery.ChunkIndex)
            .DistinctBy(result => SourceKey.From(result.Discovery))
            .Take(options.ResultCount)
            .Select(
                result => new FeatureResearchSearchResult(
                    result.Discovery,
                    result.Score,
                    result.TextRank,
                    result.VectorRank
                )
            )
            .ToList();
    }

    public static void Validate(HybridFeatureResearchSearchOptions options)
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
        if (options.TextWeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.TextWeight));
        if (options.VectorWeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.VectorWeight));
    }

    private static void AddCandidates(
        Dictionary<DocumentKey, MutableResult> results,
        List<FeatureResearchSearchCandidate> candidates,
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

    private readonly record struct DocumentKey(
        AggregateId FeatureAggregateId,
        Guid ResearchDiscoveryId,
        int ChunkIndex
    )
    {
        public static DocumentKey From(
            FeatureResearchSearchCandidate candidate
        ) =>
            new(
                candidate.FeatureAggregateId,
                candidate.ResearchDiscoveryId,
                candidate.ChunkIndex
            );
    }

    private readonly record struct SourceKey(
        AggregateId FeatureAggregateId,
        Guid ResearchDiscoveryId
    )
    {
        public static SourceKey From(
            FeatureResearchSearchCandidate candidate
        ) =>
            new(
                candidate.FeatureAggregateId,
                candidate.ResearchDiscoveryId
            );
    }

    private sealed class MutableResult(
        FeatureResearchSearchCandidate discovery
    )
    {
        public FeatureResearchSearchCandidate Discovery { get; } = discovery;
        public double Score { get; set; }
        public int? TextRank { get; set; }
        public int? VectorRank { get; set; }
    }
}
