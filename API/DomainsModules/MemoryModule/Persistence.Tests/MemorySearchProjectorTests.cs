using System.Collections.Immutable;
using System.Text.Json;
using EventSourcing.Shared.Models;
using MemoryModule.Domain;
using MemoryModule.Domain.Models;
using MemoryModule.Persistence.Interfaces;

namespace MemoryModule.Persistence.Tests;

public sealed class MemorySearchProjectorTests
{
    private static readonly AggregateId MemoryId =
        AggregateId.FromDatabaseGuid(
            Guid.Parse("11111111-1111-1111-1111-111111111111")
        );

    [Fact]
    public async Task Update_projects_each_hook_as_text_and_embedding()
    {
        var state = CreateMemory();
        var embeddingGenerator = new FakeEmbeddingGenerator();
        var repository = new FakeRepository();
        var projector = new MemorySearchProjector(
            embeddingGenerator,
            repository
        );

        await projector.Update([CreateStateInfo(state)]);

        var document = Assert.Single(repository.Documents);
        Assert.Equal(MemoryId, document.MemoryAggregateId);
        Assert.Equal("after_agent", document.HookEventName);
        Assert.Contains("important memory", document.Text);
        Assert.Equal([1f, 2f], document.Embedding.ToArray());
        Assert.Equal([document.Text], embeddingGenerator.LastInputs);
    }

    [Fact]
    public async Task Update_replaces_deleted_memory_with_empty_snapshot()
    {
        var state = CreateMemory();
        state.IsDeleted = true;
        var repository = new FakeRepository();
        var projector = new MemorySearchProjector(
            new FakeEmbeddingGenerator(),
            repository
        );

        await projector.Update([CreateStateInfo(state)]);

        Assert.Equal([MemoryId], repository.AggregateIds);
        Assert.Empty(repository.Documents);
    }

    [Fact]
    public void CompileChunks_uses_overlapping_bounded_chunks()
    {
        var prompt = CreateMemory().ChatPrompts.Values.Single();
        var hook = prompt.PromptHookRecords.Single();
        hook.Payload = JsonSerializer.SerializeToElement(
            new { value = new string('x', 4000) }
        );

        var chunks = MemoryTextChunker.CompileChunks(prompt, hook);

        Assert.True(chunks.Count > 1);
        Assert.All(
            chunks,
            chunk => Assert.InRange(
                chunk.Length,
                1,
                MemoryTextChunker.MaximumChunkLength
            )
        );
        Assert.Equal(
            chunks[0][^MemoryTextChunker.ChunkOverlapLength..],
            chunks[1][..MemoryTextChunker.ChunkOverlapLength]
        );
    }

    private static MemoryStateData CreateMemory()
    {
        var promptId = new PromptId(
            Guid.Parse("22222222-2222-2222-2222-222222222222")
        );
        var state = new MemoryStateData(MemoryId)
        {
            ThreadId = new ThreadId(
                Guid.Parse("33333333-3333-3333-3333-333333333333")
            )
        };
        state.ChatPrompts.Add(
            promptId,
            new ChatPrompt
            {
                PromptId = promptId,
                PromptStartTimestamp = new DateTime(
                    2026,
                    8,
                    8,
                    10,
                    0,
                    0,
                    DateTimeKind.Utc
                ),
                PromptHookRecords =
                [
                    new PromptHookRecord
                    {
                        HookEventName = "after_agent",
                        Payload = JsonSerializer.SerializeToElement(
                            new { value = "important memory" }
                        )
                    }
                ]
            }
        );

        return state;
    }

    private static StateInfo CreateStateInfo(MemoryStateData state) =>
        StateInfo.Create(state, "memory-state-machine", state.Id);

    private sealed class FakeEmbeddingGenerator : IMemoryEmbeddingGenerator
    {
        public IReadOnlyList<string> LastInputs { get; private set; } = [];

        public Task<IReadOnlyList<ImmutableArray<float>>> Generate(
            IReadOnlyList<string> inputs,
            CancellationToken cancellationToken = default
        )
        {
            LastInputs = inputs;
            return Task.FromResult<IReadOnlyList<ImmutableArray<float>>>(
                inputs
                    .Select(_ => ImmutableArray.Create(1f, 2f))
                    .ToList()
            );
        }
    }

    private sealed class FakeRepository : IMemorySearchRepository
    {
        public IReadOnlyCollection<AggregateId> AggregateIds { get; private set; } = [];
        public IReadOnlyCollection<MemorySearchDocument> Documents { get; private set; } = [];

        public Task Replace(
            IReadOnlyCollection<AggregateId> memoryAggregateIds,
            IReadOnlyCollection<MemorySearchDocument> documents,
            CancellationToken cancellationToken = default
        )
        {
            AggregateIds = memoryAggregateIds;
            Documents = documents;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<MemorySearchCandidate>> SearchText(
            string query,
            int candidateCount,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();

        public Task<IReadOnlyList<MemorySearchCandidate>> SearchVector(
            ImmutableArray<float> embedding,
            int candidateCount,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();
    }
}
