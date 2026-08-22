using EventSourcing.Shared.Models;
using SkillsModule.Persistence.Interfaces;

namespace SkillsModule.Persistence.Tests;

public sealed class HybridSkillRankerTests
{
    [Fact]
    public void Fuse_uses_vector_weight_to_prioritize_vector_result()
    {
        var textResult = CreateCandidate(
            "text",
            0,
            "references/text.md"
        );
        var vectorResult = CreateCandidate(
            "vector",
            1,
            "references/vector.md"
        );

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
        var textOnly = CreateCandidate(
            "text-only",
            1,
            "references/text.md"
        );
        var vectorOnly = CreateCandidate(
            "vector-only",
            2,
            "references/vector.md"
        );

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

    [Fact]
    public void Fuse_returns_highest_ranked_chunk_per_skill_source()
    {
        var bestMainChunk = CreateCandidate("best main chunk", 2);
        var lowerMainChunk = CreateCandidate("lower main chunk", 0);
        var firstReference = CreateCandidate(
            "first reference",
            1,
            "references/first.md"
        );
        var secondReference = CreateCandidate(
            "second reference",
            0,
            "references/second.md"
        );

        var results = HybridSkillRanker.Fuse(
            [
                bestMainChunk,
                firstReference,
                lowerMainChunk,
                secondReference
            ],
            [],
            new HybridSkillSearchOptions
            {
                ResultCount = 4,
                CandidateCount = 4
            }
        );

        Assert.Equal(3, results.Count);
        Assert.Equal("best main chunk", results[0].Skill.Text);
        Assert.DoesNotContain(
            results,
            result => result.Skill.Text == "lower main chunk"
        );
        Assert.Equal(
            ["SKILL.md", "references/first.md", "references/second.md"],
            results.Select(result => result.Skill.SourcePath)
        );
    }

    [Fact]
    public void Search_options_default_to_five_results()
    {
        Assert.Equal(
            5,
            new HybridSkillSearchOptions().ResultCount
        );
    }

    private static SkillSearchCandidate CreateCandidate(
        string text,
        int chunkIndex,
        string sourcePath = "SKILL.md"
    ) =>
        new(
            AggregateId.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            ),
            "event-sourcing",
            sourcePath,
            chunkIndex,
            text
        );
}
