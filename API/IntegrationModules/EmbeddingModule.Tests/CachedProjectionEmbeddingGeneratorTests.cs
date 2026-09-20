using System.Collections.Immutable;
using Xunit;

namespace EmbeddingModule.Tests;

public sealed class CachedProjectionEmbeddingGeneratorTests
{
    [Fact]
    public async Task Generate_embeds_only_unique_cache_misses_and_preserves_order()
    {
        var cache = new MemoryCache();
        var inner = new RecordingGenerator();
        var generator = CreateGenerator(cache, inner);

        var first = await generator.Generate(["same", "new", "same"]);
        var second = await generator.Generate(["new", "same"]);

        Assert.Equal(["same", "new"], Assert.Single(inner.Calls));
        Assert.Equal(first[1], second[0]);
        Assert.Equal(first[0], second[1]);
        Assert.Equal(first[0], first[2]);
    }

    [Fact]
    public async Task Generate_uses_the_model_name_as_part_of_the_hash()
    {
        var cache = new MemoryCache();
        var inner = new RecordingGenerator();

        await CreateGenerator(cache, inner, "first").Generate(["text"]);
        await CreateGenerator(cache, inner, "second").Generate(["text"]);

        Assert.Equal(2, inner.Calls.Count);
    }

    private static CachedProjectionEmbeddingGenerator CreateGenerator(
        IProjectionEmbeddingCache cache,
        ITextEmbeddingGenerator inner,
        string model = "model"
    ) =>
        new(
            cache,
            inner,
            new EmbeddingOptions
            {
                BaseUrl = new Uri("http://localhost"),
                Model = model,
                Dimensions = 2
            }
        );

    private sealed class RecordingGenerator : ITextEmbeddingGenerator
    {
        public List<List<string>> Calls { get; } = [];

        public Task<IReadOnlyList<ImmutableArray<float>>> Generate(
            IReadOnlyList<string> inputs,
            CancellationToken cancellationToken = default
        )
        {
            Calls.Add(inputs.ToList());
            return Task.FromResult<IReadOnlyList<ImmutableArray<float>>>(
                inputs.Select(
                    (_, index) => ImmutableArray.Create(
                        (float)(Calls.Count * 10 + index),
                        1f
                    )
                ).ToList()
            );
        }
    }

    private sealed class MemoryCache : IProjectionEmbeddingCache
    {
        private readonly Dictionary<string, ImmutableArray<float>> _items = [];

        public Task<IReadOnlyDictionary<string, ImmutableArray<float>>> Get(
            List<string> textHashes,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult<IReadOnlyDictionary<string, ImmutableArray<float>>>(
                _items
                    .Where(item => textHashes.Contains(item.Key))
                    .ToDictionary()
            );

        public Task Add(
            List<CachedEmbedding> embeddings,
            CancellationToken cancellationToken = default
        )
        {
            foreach (var embedding in embeddings)
                _items.Add(embedding.TextHash, embedding.Embedding);

            return Task.CompletedTask;
        }
    }
}
