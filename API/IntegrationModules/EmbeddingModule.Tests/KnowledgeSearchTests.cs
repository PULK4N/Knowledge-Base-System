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
                )
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
        List<KnowledgeSearchCandidate> vectorCandidates
    ) : IKnowledgeSearchRepository
    {
        public int LastCandidateCount { get; private set; }

        public Task Write(string ownerType, List<AggregateId> ownerAggregateIds, List<KnowledgeSearchDocument> documents, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<List<KnowledgeSearchCandidate>> SearchText(string query, int candidateCount, CancellationToken cancellationToken = default)
        {
            LastCandidateCount = candidateCount;
            return Task.FromResult(textCandidates);
        }

        public Task<List<KnowledgeSearchCandidate>> SearchVector(ImmutableArray<float> embedding, int candidateCount, CancellationToken cancellationToken = default)
        {
            LastCandidateCount = candidateCount;
            return Task.FromResult(vectorCandidates);
        }
    }
}
