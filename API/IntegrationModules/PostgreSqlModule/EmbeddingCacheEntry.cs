using Pgvector;

namespace PostgreSqlModule;

internal sealed class EmbeddingCacheEntry
{
    public required string TextHash { get; set; }
    public required Vector Embedding { get; set; }
}
