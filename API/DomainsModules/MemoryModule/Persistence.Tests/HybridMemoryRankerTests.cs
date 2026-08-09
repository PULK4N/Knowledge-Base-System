using EventSourcing.Shared.Models;
using MemoryModule.Domain.Models;
using MemoryModule.Persistence.Interfaces;

namespace MemoryModule.Persistence.Tests;

public sealed class HybridMemoryRankerTests
{
    [Fact]
    public void Fuse_uses_vector_weight_to_prioritize_vector_result()
    {
        var textResult = CreateCandidate(
            "11111111-1111-1111-1111-111111111111",
            "text"
        );
        var vectorResult = CreateCandidate(
            "22222222-2222-2222-2222-222222222222",
            "vector"
        );

        var results = HybridMemoryRanker.Fuse(
            [textResult],
            [vectorResult],
            new HybridMemorySearchOptions
            {
                ResultCount = 2,
                CandidateCount = 2,
                TextWeight = 1,
                VectorWeight = 2
            }
        );

        Assert.Equal("vector", results[0].Memory.Text);
        Assert.Null(results[0].TextRank);
        Assert.Equal(1, results[0].VectorRank);
        Assert.True(results[0].Score > results[1].Score);
    }

    [Fact]
    public void Fuse_rewards_result_found_by_both_searches()
    {
        var shared = CreateCandidate(
            "11111111-1111-1111-1111-111111111111",
            "shared"
        );
        var textOnly = CreateCandidate(
            "22222222-2222-2222-2222-222222222222",
            "text-only"
        );
        var vectorOnly = CreateCandidate(
            "33333333-3333-3333-3333-333333333333",
            "vector-only"
        );

        var results = HybridMemoryRanker.Fuse(
            [textOnly, shared],
            [vectorOnly, shared],
            new HybridMemorySearchOptions
            {
                ResultCount = 3,
                CandidateCount = 3
            }
        );

        Assert.Equal("shared", results[0].Memory.Text);
        Assert.Equal(2, results[0].TextRank);
        Assert.Equal(2, results[0].VectorRank);
    }

    private static MemorySearchCandidate CreateCandidate(
        string promptId,
        string text
    ) =>
        new(
            AggregateId.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            ),
            new ThreadId(
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
            ),
            new PromptId(Guid.Parse(promptId)),
            0,
            0,
            DateTime.UnixEpoch,
            "hook",
            text
        );
}
