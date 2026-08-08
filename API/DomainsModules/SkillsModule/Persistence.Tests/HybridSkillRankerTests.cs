using EventSourcing.Shared.Models;
using SkillsModule.Persistence.Interfaces;

namespace SkillsModule.Persistence.Tests;

public sealed class HybridSkillRankerTests
{
    [Fact]
    public void Fuse_uses_vector_weight_to_prioritize_vector_result()
    {
        var textResult = CreateCandidate("text", 0);
        var vectorResult = CreateCandidate("vector", 1);

        var results = HybridSkillRanker.Fuse(
            [textResult],
            [vectorResult],
            new HybridSkillSearchOptions
            {
                ResultCount = 2,
                CandidateCount = 2,
                TextWeight = 1,
                VectorWeight = 2
            }
        );

        Assert.Equal("vector", results[0].Skill.Text);
        Assert.Null(results[0].TextRank);
        Assert.Equal(1, results[0].VectorRank);
        Assert.True(results[0].Score > results[1].Score);
    }

    [Fact]
    public void Fuse_rewards_chunk_found_by_both_searches()
    {
        var shared = CreateCandidate("shared", 0);
        var textOnly = CreateCandidate("text-only", 1);
        var vectorOnly = CreateCandidate("vector-only", 2);

        var results = HybridSkillRanker.Fuse(
            [textOnly, shared],
            [vectorOnly, shared],
            new HybridSkillSearchOptions
            {
                ResultCount = 3,
                CandidateCount = 3
            }
        );

        Assert.Equal("shared", results[0].Skill.Text);
        Assert.Equal(2, results[0].TextRank);
        Assert.Equal(2, results[0].VectorRank);
    }

    private static SkillSearchCandidate CreateCandidate(
        string text,
        int chunkIndex
    ) =>
        new(
            AggregateId.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            ),
            "event-sourcing",
            "SKILL.md",
            chunkIndex,
            text
        );
}
