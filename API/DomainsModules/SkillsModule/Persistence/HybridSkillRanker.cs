using EventSourcing.Shared.Models;
using SkillsModule.Persistence.Interfaces;

namespace SkillsModule.Persistence;

public static class HybridSkillRanker
{
    public const int RankConstant = 60;

    public static IReadOnlyList<SkillSearchResult> Fuse(
        IReadOnlyList<SkillSearchCandidate> textCandidates,
        IReadOnlyList<SkillSearchCandidate> vectorCandidates,
        HybridSkillSearchOptions options
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
            .ThenBy(result => result.Skill.SkillName, StringComparer.Ordinal)
            .ThenBy(result => result.Skill.SourcePath, StringComparer.Ordinal)
            .ThenBy(result => result.Skill.ChunkIndex)
            .Take(options.ResultCount)
            .Select(
                result => new SkillSearchResult(
                    result.Skill,
                    result.Score,
                    result.TextRank,
                    result.VectorRank
                )
            )
            .ToList();
    }

    public static void Validate(HybridSkillSearchOptions options)
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
        IReadOnlyList<SkillSearchCandidate> candidates,
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
        AggregateId SkillAggregateId,
        string SourcePath,
        int ChunkIndex
    )
    {
        public static DocumentKey From(SkillSearchCandidate candidate) =>
            new(
                candidate.SkillAggregateId,
                candidate.SourcePath,
                candidate.ChunkIndex
            );
    }

    private sealed class MutableResult(SkillSearchCandidate skill)
    {
        public SkillSearchCandidate Skill { get; } = skill;
        public double Score { get; set; }
        public int? TextRank { get; set; }
        public int? VectorRank { get; set; }
    }
}
