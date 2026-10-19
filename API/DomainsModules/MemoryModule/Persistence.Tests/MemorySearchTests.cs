using System.Collections.Immutable;
using EmbeddingModule;
using EventSourcing.Shared.Models;
using MemoryModule.Persistence.Interfaces;

namespace MemoryModule.Persistence.Tests;

public sealed class MemorySearchTests
{
    [Fact]
    public async Task SearchWithSources_passes_the_time_range_and_model_to_every_search()
    {
        var timeRange = new MemorySearchTimeRange(
            DateTime.UnixEpoch,
            DateTime.UnixEpoch.AddDays(1)
        );
        var repository = new CapturingRepository();
        var search = new MemorySearch(new FixedEmbeddingGenerator(), repository);

        await search.SearchWithSources(
            "outbox retries",
            ["outbox"],
            new HybridMemorySearchOptions
            {
                TimeRange = timeRange,
                Model = "claude-opus-5-5"
            }
        );

        Assert.Equal(4, repository.Filters.Count);
        Assert.All(
            repository.Filters,
            captured => Assert.Equal(
                new MemorySearchFilter(timeRange, "claude-opus-5-5"),
                captured
            )
        );
    }

    [Theory]
    [InlineData("outbox retries", new[] { "outbox" }, new[] { "SearchText", "SearchTextBySource", "SearchVector", "SearchVectorBySource" })]
    [InlineData("outbox retries", new string[0], new[] { "SearchVector", "SearchVectorBySource" })]
    [InlineData(" ", new[] { "outbox" }, new[] { "SearchText", "SearchTextBySource" })]
    [InlineData(null, new[] { "outbox" }, new[] { "SearchText", "SearchTextBySource" })]
    public async Task SearchWithSources_skips_the_leg_without_input(
        string? query,
        string[] keywords,
        string[] expectedCalls
    )
    {
        var repository = new CapturingRepository();
        var embeddings = new FixedEmbeddingGenerator();
        var search = new MemorySearch(embeddings, repository);

        await search.SearchWithSources(query, keywords.ToList());

        Assert.Equal(expectedCalls, repository.Calls);
        Assert.Equal(
            expectedCalls.Contains("SearchVector") ? 1 : 0,
            embeddings.CallCount
        );
    }

    [Fact]
    public async Task SearchWithSources_requires_a_query_or_keywords()
    {
        var search = new MemorySearch(
            new FixedEmbeddingGenerator(),
            new CapturingRepository()
        );

        await Assert.ThrowsAsync<ArgumentException>(
            () => search.SearchWithSources("  ", [])
        );
    }

    private sealed class FixedEmbeddingGenerator : ITextEmbeddingGenerator
    {
        public int CallCount { get; private set; }

        public Task<IReadOnlyList<ImmutableArray<float>>> Generate(
            IReadOnlyList<string> inputs,
            CancellationToken cancellationToken = default
        )
        {
            CallCount++;
            return Task.FromResult<IReadOnlyList<ImmutableArray<float>>>(
                inputs.Select(_ => ImmutableArray.Create(1f, 2f)).ToList()
            );
        }
    }

    private sealed class CapturingRepository : IMemorySearchRepository
    {
        public List<MemorySearchFilter?> Filters { get; } = [];
        public List<string> Calls { get; } = [];

        public Task Write(
            IReadOnlyCollection<AggregateId> memoryAggregateIds,
            IReadOnlyCollection<MemorySearchDocument> documents,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();

        public Task<IReadOnlyList<MemorySearchCandidate>> SearchText(
            List<string> keywords,
            int candidateCount,
            MemorySearchFilter? filter = null,
            CancellationToken cancellationToken = default
        ) => Capture(nameof(SearchText), filter);

        public Task<IReadOnlyList<MemorySearchCandidate>> SearchVector(
            ImmutableArray<float> embedding,
            int candidateCount,
            MemorySearchFilter? filter = null,
            CancellationToken cancellationToken = default
        ) => Capture(nameof(SearchVector), filter);

        public Task<IReadOnlyList<MemorySearchCandidate>> SearchTextBySource(
            List<string> keywords,
            int sourceCount,
            int candidateCount,
            MemorySearchFilter? filter = null,
            CancellationToken cancellationToken = default
        ) => Capture(nameof(SearchTextBySource), filter);

        public Task<IReadOnlyList<MemorySearchCandidate>> SearchVectorBySource(
            ImmutableArray<float> embedding,
            int sourceCount,
            int candidateCount,
            MemorySearchFilter? filter = null,
            CancellationToken cancellationToken = default
        ) => Capture(nameof(SearchVectorBySource), filter);

        private Task<IReadOnlyList<MemorySearchCandidate>> Capture(
            string call,
            MemorySearchFilter? filter
        )
        {
            Calls.Add(call);
            Filters.Add(filter);
            return Task.FromResult<IReadOnlyList<MemorySearchCandidate>>([]);
        }
    }
}
