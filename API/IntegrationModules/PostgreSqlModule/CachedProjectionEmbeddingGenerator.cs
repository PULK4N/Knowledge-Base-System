using System.Collections.Immutable;
using EmbeddingModule;
using EventSourcing.Persistence;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace PostgreSqlModule;

internal sealed class PostgreSqlProjectionEmbeddingCache(
    EventSourcingDbContext dbContext
) : IProjectionEmbeddingCache
{
    public async Task<IReadOnlyDictionary<string, ImmutableArray<float>>> Get(
        List<string> textHashes,
        CancellationToken cancellationToken = default
    ) =>
        await dbContext.Set<EmbeddingCacheEntry>()
            .AsNoTracking()
            .Where(entry => textHashes.Contains(entry.TextHash))
            .ToDictionaryAsync(
                entry => entry.TextHash,
                entry => entry.Embedding.ToArray().ToImmutableArray(),
                StringComparer.Ordinal,
                cancellationToken
            );

    public async Task Add(
        List<CachedEmbedding> embeddings,
        CancellationToken cancellationToken = default
    )
    {
        dbContext.Set<EmbeddingCacheEntry>().AddRange(
            embeddings.Select(
                embedding => new EmbeddingCacheEntry
                {
                    TextHash = embedding.TextHash,
                    Embedding = new Vector(embedding.Embedding.ToArray())
                }
            )
        );
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
