using System.Collections.Immutable;
using EmbeddingModule;
using EventSourcing.Shared.Models;
using MemoryModule.Persistence.Interfaces;

namespace MemoryModule.Persistence.Tests;

public sealed class MemorySearchTests
{
    [Fact]
    public async Task SearchWithSources_passes_the_time_range_to_every_search()
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
            new HybridMemorySearchOptions { TimeRange = timeRange }
        );

        Assert.Equal(4, repository.TimeRanges.Count);
        Assert.All(
            repository.TimeRanges,
            captured => Assert.Equal(timeRange, captured)
        );
    }

    private sealed class FixedEmbeddingGenerator : ITextEmbeddingGenerator
    {
        public Task<IReadOnlyList<ImmutableArray<float>>> Generate(
            IReadOnlyList<string> inputs,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult<IReadOnlyList<ImmutableArray<float>>>(
                inputs.Select(_ => ImmutableArray.Create(1f, 2f)).ToList()
            );
    }

    private sealed class CapturingRepository : IMemorySearchRepository
    {
        public List<MemorySearchTimeRange?> TimeRanges { get; } = [];

        public Task Write(
            IReadOnlyCollection<AggregateId> memoryAggregateIds,
            IReadOnlyCollection<MemorySearchDocument> documents,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();

        public Task<IReadOnlyList<MemorySearchCandidate>> SearchText(
            List<string> keywords,
            int candidateCount,
            MemorySearchTimeRange? timeRange = null,
            CancellationToken cancellationToken = default
        ) => Capture(timeRange);

        public Task<IReadOnlyList<MemorySearchCandidate>> SearchVector(
            ImmutableArray<float> embedding,
            int candidateCount,
            MemorySearchTimeRange? timeRange = null,
            CancellationToken cancellationToken = default
        ) => Capture(timeRange);

        public Task<IReadOnlyList<MemorySearchCandidate>> SearchTextBySource(
            List<string> keywords,
            int sourceCount,
            int candidateCount,
            MemorySearchTimeRange? timeRange = null,
            CancellationToken cancellationToken = default
        ) => Capture(timeRange);

        public Task<IReadOnlyList<MemorySearchCandidate>> SearchVectorBySource(
            ImmutableArray<float> embedding,
            int sourceCount,
            int candidateCount,
            MemorySearchTimeRange? timeRange = null,
            CancellationToken cancellationToken = default
        ) => Capture(timeRange);

        private Task<IReadOnlyList<MemorySearchCandidate>> Capture(
            MemorySearchTimeRange? timeRange
        )
        {
            TimeRanges.Add(timeRange);
            return Task.FromResult<IReadOnlyList<MemorySearchCandidate>>([]);
        }
    }
}
