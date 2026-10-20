using EventSourcing.Shared.Models;
using FeatureModule.Persistence.Interfaces;

namespace FeatureModule.Persistence.Tests;

public sealed class HybridFeatureRecordRankerTests
{
    [Fact]
    public void Fuse_rewards_a_chunk_found_by_both_searches()
    {
        var shared = CreateCandidate("shared", Guid.NewGuid(), 0);
        var textOnly = CreateCandidate("text", Guid.NewGuid(), 0);
        var vectorOnly = CreateCandidate("vector", Guid.NewGuid(), 0);

        var results = HybridFeatureRecordRanker.Fuse(
            [textOnly, shared],
            [vectorOnly, shared],
            new HybridFeatureRecordSearchOptions
            {
                ResultCount = 3,
                CandidateCount = 3
            }
        );

        Assert.Equal("shared", results[0].Record.Text);
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

        var results = HybridFeatureRecordRanker.Fuse(
            [best, other, lower],
            [],
            new HybridFeatureRecordSearchOptions
            {
                ResultCount = 3,
                CandidateCount = 3
            }
        );

        Assert.Equal(2, results.Count);
        Assert.Equal("best", results[0].Record.Text);
        Assert.DoesNotContain(
            results,
            result => result.Record.Text == "lower"
        );
    }

    [Fact]
    public void FuseSources_keeps_one_discovery_per_feature()
    {
        var otherFeatureId = AggregateId.FromDatabaseGuid(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
        );
        var first = CreateCandidate("first", Guid.NewGuid(), 0);
        var repeatedFeature = CreateCandidate(
            "repeated",
            Guid.NewGuid(),
            0
        );
        var otherFeature = CreateCandidate("other", Guid.NewGuid(), 0) with
        {
            FeatureAggregateId = otherFeatureId,
            FeatureName = "Other feature"
        };

        var results = HybridFeatureRecordRanker.FuseSources(
            [first, repeatedFeature, otherFeature],
            [],
            new HybridFeatureRecordSearchOptions
            {
                ResultCount = 5,
                CandidateCount = 5,
                SourceResultCount = 5,
                SourceCandidateCount = 5
            }
        );

        Assert.Equal(2, results.Count);
        Assert.Equal("first", results[0].Record.Text);
        Assert.Equal(1, results[0].TextRank);
        Assert.Equal(
            otherFeatureId,
            results[1].Record.FeatureAggregateId
        );
    }

    private static FeatureRecordSearchCandidate CreateCandidate(
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
            "research",
            "Discovery",
            "Code",
            "API/file.cs",
            DateTime.UnixEpoch,
            chunkIndex,
            text
        );
}
