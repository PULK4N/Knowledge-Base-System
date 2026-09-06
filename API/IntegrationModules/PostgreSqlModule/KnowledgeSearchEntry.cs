using NpgsqlTypes;
using Pgvector;

namespace PostgreSqlModule;

internal sealed class KnowledgeSearchEntry
{
    public int Id { get; set; }
    public required string OwnerType { get; set; }
    public Guid OwnerAggregateId { get; set; }
    public required string SourceType { get; set; }
    public required string SourceKey { get; set; }
    public int ChunkIndex { get; set; }
    public DateTime? Timestamp { get; set; }
    public required string MetadataJson { get; set; }
    public required string SearchableMetadata { get; set; }
    public required string Text { get; set; }
    public required Vector Embedding { get; set; }
    public NpgsqlTsVector SearchVector { get; set; } = null!;
}
