using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;

namespace EmbeddingModule;

public sealed record CachedEmbedding(
    string TextHash,
    ImmutableArray<float> Embedding
);

public interface IProjectionEmbeddingCache
{
    Task<IReadOnlyDictionary<string, ImmutableArray<float>>> Get(
        List<string> textHashes,
        CancellationToken cancellationToken = default
    );

    Task Add(
        List<CachedEmbedding> embeddings,
        CancellationToken cancellationToken = default
    );
}

public sealed class CachedProjectionEmbeddingGenerator(
    IProjectionEmbeddingCache cache,
    ITextEmbeddingGenerator embeddingGenerator,
    EmbeddingOptions options
) : IProjectionEmbeddingGenerator
{
    public async Task<IReadOnlyList<ImmutableArray<float>>> Generate(
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken = default
    )
    {
        if (inputs.Count == 0)
            return [];

        var inputsByHash = inputs
            .Select(input => new HashedInput(CreateHash(input), input))
            .GroupBy(input => input.Hash, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.First().Text,
                StringComparer.Ordinal
            );
        var embeddings = new Dictionary<string, ImmutableArray<float>>(
            await cache.Get(inputsByHash.Keys.ToList(), cancellationToken),
            StringComparer.Ordinal
        );
        var missingHashes = inputsByHash.Keys
            .Where(hash => !embeddings.ContainsKey(hash))
            .ToList();

        if (missingHashes.Count > 0)
        {
            var generated = await embeddingGenerator.Generate(
                missingHashes.Select(hash => inputsByHash[hash]).ToList(),
                cancellationToken
            );
            var additions = missingHashes
                .Select(
                    (hash, index) => new CachedEmbedding(
                        hash,
                        generated[index]
                    )
                )
                .ToList();

            await cache.Add(additions, cancellationToken);
            foreach (var addition in additions)
                embeddings.Add(addition.TextHash, addition.Embedding);
        }

        return inputs
            .Select(input => embeddings[CreateHash(input)])
            .ToList();
    }

    private string CreateHash(string text)
    {
        var bytes = Encoding.UTF8.GetBytes($"{options.Model}\0{text}");
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private sealed record HashedInput(string Hash, string Text);
}
