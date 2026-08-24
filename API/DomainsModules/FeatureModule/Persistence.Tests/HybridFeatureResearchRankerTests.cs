using EventSourcing.Shared.Models;
using FeatureModule.Persistence.Interfaces;

namespace FeatureModule.Persistence.Tests;

public sealed class HybridFeatureResearchRankerTests
{
    [Fact]
    public void Fuse_rewards_a_chunk_found_by_both_searches()
    {
        var shared = CreateCandidate("shared", Guid.NewGuid(), 0);
        var textOnly = CreateCandidate("text", Guid.NewGuid(), 0);
        var vectorOnly = CreateCandidate("vector", Guid.NewGuid(), 0);

        var results = HybridFeatureResearchRanker.Fuse(
            [textOnly, shared],
            [vectorOnly, shared],
            new HybridFeatureResearchSearchOptions
            {
                ResultCount = 3,
                CandidateCount = 3
            }
        );

        Assert.Equal("shared", results[0].ResearchDiscovery.Text);
        Assert.Equal(2, results[0].TextRank);
        Assert.Equal(2, results[0].VectorRank);
    }

    [Fact]
    public void Fuse_returns_only_the_highest_ranked_chunk_per_discovery()
    {
        var discoveryId = Guid.NewGuid();
        var best = CreateCandidate("best", discoveryId, 2);
        var lower = CreateCandidate("lower", discoveryId, 0);
        var other = CreateCandidate("other", Guid.NewGuid(), 0);

        var results = HybridFeatureResearchRanker.Fuse(
            [best, other, lower],
            [],
            new HybridFeatureResearchSearchOptions
            {
                ResultCount = 3,
                CandidateCount = 3
            }
        );

        Assert.Equal(2, results.Count);
        Assert.Equal("best", results[0].ResearchDiscovery.Text);
        Assert.DoesNotContain(
            results,
            result => result.ResearchDiscovery.Text == "lower"
        );
    }

    private static FeatureResearchSearchCandidate CreateCandidate(
        string text,
        Guid discoveryId,
        int chunkIndex
    ) =>
        new(
            AggregateId.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            ),
            "Feature",
            discoveryId,
            "Discovery",
            "Code",
            "API/file.cs",
            DateTime.UnixEpoch,
            chunkIndex,
            text
        );
}
