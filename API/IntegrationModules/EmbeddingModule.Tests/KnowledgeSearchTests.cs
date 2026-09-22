using System.Collections.Immutable;
using System.Text.Json;
using EventSourcing.Shared.Models;
using Xunit;

namespace EmbeddingModule.Tests;

public sealed class KnowledgeSearchTests
{
    [Fact]
    public async Task Search_rejects_oversized_queries_before_embedding()
    {
        var generator = new FakeEmbeddingGenerator();
        var search = new KnowledgeSearch(
            generator,
            new FakeRepository([], [])
        );

        await Assert.ThrowsAsync<ArgumentException>(
            () => search.Search(
                new string(
                    'x',
                    KnowledgeSearchQueryLimits.MaximumLength + 1
                ),
                ["query"]
            )
        );

        Assert.Equal(0, generator.CallCount);
    }

    [Fact]
    public async Task Search_generates_one_query_embedding_and_returns_one_best_chunk_per_source()
    {
        var ownerId = AggregateId.FromDatabaseGuid(
            Guid.Parse("11111111-1111-1111-1111-111111111111")
        );
        var metadata = JsonSerializer.SerializeToElement(
            new { featureId = ownerId.Value }
        );
        var firstChunk = new KnowledgeSearchCandidate(
            1,
            KnowledgeSearchOwnerTypes.Feature,
            ownerId,
            KnowledgeSearchSourceTypes.FeaturePlan,
            "plan",
            0,
            DateTime.UnixEpoch,
            metadata,
            "first"
        );
        var secondChunk = firstChunk with
        {
            Id = 2,
            ChunkIndex = 1,
            Text = "second"
        };
        var memory = firstChunk with
        {
            Id = 3,
            OwnerType = KnowledgeSearchOwnerTypes.Memory,
            OwnerAggregateId = AggregateId.FromDatabaseGuid(Guid.NewGuid()),
            SourceType = KnowledgeSearchSourceTypes.MemorySummary,
            SourceKey = "summary",
            Text = "memory"
        };
        var generator = new FakeEmbeddingGenerator();
        var repository = new FakeRepository(
            [firstChunk, secondChunk, memory],
            [firstChunk, memory, secondChunk]
        );
        var search = new KnowledgeSearch(generator, repository);

        var results = await search.Search(
            "architecture",
            ["architecture"],
            new HybridKnowledgeSearchOptions
            {
                ResultCount = 2,
                CandidateCount = 3
            }
        );

        Assert.Equal(1, generator.CallCount);
        Assert.Equal(["architecture"], generator.Inputs);
        Assert.Equal(2, results.Count);
        Assert.Single(
            results.Where(
                result => result.Document.SourceType
                    == KnowledgeSearchSourceTypes.FeaturePlan
            )
        );
        Assert.Equal(3, repository.LastCandidateCount);
    }

    [Fact]
    public async Task SearchWithSources_returns_top_matches_and_one_chunk_per_owner()
    {
        var featureId = AggregateId.FromDatabaseGuid(
            Guid.Parse("11111111-1111-1111-1111-111111111111")
        );
        var metadata = JsonSerializer.SerializeToElement(
            new { featureId = featureId.Value }
        );
        var featurePlan = new KnowledgeSearchCandidate(
            1,
            KnowledgeSearchOwnerTypes.Feature,
            featureId,
            KnowledgeSearchSourceTypes.FeaturePlan,
            "plan",
            0,
            DateTime.UnixEpoch,
            metadata,
            "plan chunk"
        );
        var featureRecord = featurePlan with
        {
            Id = 2,
            SourceType = KnowledgeSearchSourceTypes.FeatureRecord,
            SourceKey = "record",
            Text = "record chunk"
        };
        var skill = featurePlan with
        {
            Id = 3,
            OwnerType = KnowledgeSearchOwnerTypes.Skill,
            OwnerAggregateId = AggregateId.FromDatabaseGuid(
                Guid.Parse("22222222-2222-2222-2222-222222222222")
            ),
            SourceType = KnowledgeSearchSourceTypes.Skill,
            SourceKey = "skill.md",
            Text = "skill chunk"
        };
        var generator = new FakeEmbeddingGenerator();
        // The ranked window is filled by one feature; the source queries
        // return one row per owner.
        var repository = new FakeRepository(
            [featurePlan, featureRecord],
            [featurePlan, featureRecord],
            [featurePlan, skill],
            [featurePlan, skill]
        );
        var search = new KnowledgeSearch(generator, repository);

        var results = await search.SearchWithSources(
            "architecture",
            ["architecture"],
            new HybridKnowledgeSearchOptions
            {
                ResultCount = 2,
                CandidateCount = 3,
                SourceResultCount = 2,
                SourceCandidateCount = 10
            }
        );

        Assert.Equal(1, generator.CallCount);
        Assert.Equal(
            [featureId.Value, featureId.Value],
            results.TopMatches
                .Select(result => result.Document.OwnerAggregateId.Value)
                .ToList()
        );
        Assert.Equal(
            [
                KnowledgeSearchOwnerTypes.Feature,
                KnowledgeSearchOwnerTypes.Skill
            ],
            results.DistinctSources
                .Select(result => result.Document.OwnerType)
                .Order(StringComparer.Ordinal)
                .ToList()
        );
        Assert.Equal(2, repository.LastSourceCount);
        Assert.Equal(10, repository.LastSourceCandidateCount);
    }

    [Fact]
    public async Task SearchWithSources_scores_one_owner_once_per_candidate_list()
    {
        var ownerId = AggregateId.FromDatabaseGuid(
            Guid.Parse("33333333-3333-3333-3333-333333333333")
        );
        var chunk = new KnowledgeSearchCandidate(
            1,
            KnowledgeSearchOwnerTypes.Skill,
            ownerId,
            KnowledgeSearchSourceTypes.Skill,
            "skill.md",
            0,
            DateTime.UnixEpoch,
            JsonSerializer.SerializeToElement(new { }),
            "first"
        );
        var duplicateOwnerChunk = chunk with { Id = 2, ChunkIndex = 1 };
        var repository = new FakeRepository(
            [],
            [],
            [chunk, duplicateOwnerChunk],
            []
        );
        var search = new KnowledgeSearch(
            new FakeEmbeddingGenerator(),
            repository
        );

        var results = await search.SearchWithSources(
            "skill",
            ["skill"],
            new HybridKnowledgeSearchOptions
            {
                ResultCount = 2,
                CandidateCount = 3,
                SourceResultCount = 2,
                SourceCandidateCount = 10
            }
        );

        var single = Assert.Single(results.DistinctSources);
        Assert.Equal(1, single.TextRank);
        Assert.Null(single.VectorRank);
    }

    [Fact]
    public async Task Search_rejects_an_empty_keyword_list_before_embedding()
    {
        var generator = new FakeEmbeddingGenerator();
        var search = new KnowledgeSearch(
            generator,
            new FakeRepository([], [])
        );

        await Assert.ThrowsAsync<ArgumentException>(
            () => search.Search("a meaningful sentence", [])
        );

        Assert.Equal(0, generator.CallCount);
    }

    [Fact]
    public async Task SearchWithSources_sends_keywords_to_the_text_leg_only()
    {
        var repository = new FakeRepository([], [], [], []);
        var generator = new FakeEmbeddingGenerator();
        var search = new KnowledgeSearch(generator, repository);

        await search.SearchWithSources(
            "how do I configure routing",
            ["routing", "configure"],
            new HybridKnowledgeSearchOptions
            {
                ResultCount = 2,
                CandidateCount = 3,
                SourceResultCount = 2,
                SourceCandidateCount = 10
            }
        );

        Assert.Equal(["routing", "configure"], repository.LastKeywords);
        Assert.Equal(["how do I configure routing"], generator.Inputs);
    }

    private sealed class FakeEmbeddingGenerator : ITextEmbeddingGenerator
    {
        public int CallCount { get; private set; }
        public IReadOnlyList<string> Inputs { get; private set; } = [];

        public Task<IReadOnlyList<ImmutableArray<float>>> Generate(
            IReadOnlyList<string> inputs,
            CancellationToken cancellationToken = default
        )
        {
            CallCount++;
            Inputs = inputs;
            return Task.FromResult<IReadOnlyList<ImmutableArray<float>>>(
                [ImmutableArray.Create(1f, 2f)]
            );
        }
    }

    private sealed class FakeRepository(
        List<KnowledgeSearchCandidate> textCandidates,
        List<KnowledgeSearchCandidate> vectorCandidates,
        List<KnowledgeSearchCandidate>? textSourceCandidates = null,
        List<KnowledgeSearchCandidate>? vectorSourceCandidates = null
    ) : IKnowledgeSearchRepository
    {
        public int LastCandidateCount { get; private set; }
        public List<string> LastKeywords { get; private set; } = [];
        public int LastSourceCount { get; private set; }
        public int LastSourceCandidateCount { get; private set; }

        public Task Write(string ownerType, List<AggregateId> ownerAggregateIds, List<KnowledgeSearchDocument> documents, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<List<KnowledgeSearchCandidate>> SearchText(List<string> keywords, int candidateCount, CancellationToken cancellationToken = default)
        {
            LastKeywords = keywords;
            LastCandidateCount = candidateCount;
            return Task.FromResult(textCandidates);
        }

        public Task<List<KnowledgeSearchCandidate>> SearchVector(ImmutableArray<float> embedding, int candidateCount, CancellationToken cancellationToken = default)
        {
            LastCandidateCount = candidateCount;
            return Task.FromResult(vectorCandidates);
        }

        public Task<List<KnowledgeSearchCandidate>> SearchTextBySource(List<string> keywords, int sourceCount, int candidateCount, CancellationToken cancellationToken = default)
        {
            LastKeywords = keywords;
            LastSourceCount = sourceCount;
            LastSourceCandidateCount = candidateCount;
            return Task.FromResult(textSourceCandidates ?? []);
        }

        public Task<List<KnowledgeSearchCandidate>> SearchVectorBySource(ImmutableArray<float> embedding, int sourceCount, int candidateCount, CancellationToken cancellationToken = default)
        {
            LastSourceCount = sourceCount;
            LastSourceCandidateCount = candidateCount;
            return Task.FromResult(vectorSourceCandidates ?? []);
        }
    }
}
