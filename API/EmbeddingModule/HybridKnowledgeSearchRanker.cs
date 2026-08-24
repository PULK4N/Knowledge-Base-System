namespace EmbeddingModule;

internal static class HybridKnowledgeSearchRanker
{
    private const double ReciprocalRankConstant = 60;

    internal static List<KnowledgeSearchResult> Rank(
        List<KnowledgeSearchCandidate> textCandidates,
        List<KnowledgeSearchCandidate> vectorCandidates,
        HybridKnowledgeSearchOptions options
    )
    {
        Validate(options);

        var ranked = new Dictionary<DocumentKey, RankedDocument>();
        AddCandidates(ranked, textCandidates, options.TextWeight, true);
        AddCandidates(ranked, vectorCandidates, options.VectorWeight, false);

        return ranked.Values
            .OrderByDescending(result => result.Score)
            .ThenByDescending(result => result.Candidate.Timestamp)
            .ThenBy(result => result.Candidate.Id)
            .GroupBy(
                result => new SourceKey(
                    result.Candidate.OwnerType,
                    result.Candidate.OwnerAggregateId.Value,
                    result.Candidate.SourceType,
                    result.Candidate.SourceKey
                )
            )
            .Select(group => group.First())
            .Take(options.ResultCount)
            .Select(
                result => new KnowledgeSearchResult(
                    result.Candidate,
                    result.Score,
                    result.TextRank,
                    result.VectorRank
                )
            )
            .ToList();
    }

    internal static void Validate(HybridKnowledgeSearchOptions options)
    {
        if (options.ResultCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.ResultCount));
        if (options.CandidateCount < options.ResultCount)
            throw new ArgumentOutOfRangeException(nameof(options.CandidateCount));
        if (options.CandidateCount
            > HybridKnowledgeSearchOptions.MaximumCandidateCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.CandidateCount)
            );
        }
        if (options.TextWeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.TextWeight));
        if (options.VectorWeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.VectorWeight));
    }

    private static void AddCandidates(
        Dictionary<DocumentKey, RankedDocument> ranked,
        List<KnowledgeSearchCandidate> candidates,
        double weight,
        bool isText
    )
    {
        for (var index = 0; index < candidates.Count; index++)
        {
            var candidate = candidates[index];
            var key = new DocumentKey(
                candidate.OwnerType,
                candidate.OwnerAggregateId.Value,
                candidate.SourceType,
                candidate.SourceKey,
                candidate.ChunkIndex
            );

            if (!ranked.TryGetValue(key, out var result))
            {
                result = new RankedDocument(candidate);
                ranked.Add(key, result);
            }

            var rank = index + 1;
            result.Score += weight / (ReciprocalRankConstant + rank);
            if (isText)
                result.TextRank = rank;
            else
                result.VectorRank = rank;
        }
    }

    private readonly record struct DocumentKey(
        string OwnerType,
        Guid OwnerId,
        string SourceType,
        string SourceKey,
        int ChunkIndex
    );

    private readonly record struct SourceKey(
        string OwnerType,
        Guid OwnerId,
        string SourceType,
        string Value
    );

    private sealed class RankedDocument(
        KnowledgeSearchCandidate candidate
    )
    {
        public KnowledgeSearchCandidate Candidate { get; } = candidate;
        public double Score { get; set; }
        public int? TextRank { get; set; }
        public int? VectorRank { get; set; }
    }
}
