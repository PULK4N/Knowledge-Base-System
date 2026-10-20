using NpgsqlTypes;
using Pgvector;

namespace PostgreSqlModule;

internal sealed class FeatureRecordSearchEntry
{
    public Guid FeatureAggregateId { get; set; }
    public required string FeatureName { get; set; }
    public Guid RecordId { get; set; }
    public required string RecordType { get; set; }
    public required string Title { get; set; }
    public required string SourceType { get; set; }
    public required string SourceReference { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int ChunkIndex { get; set; }
    public required string Text { get; set; }
    public required Vector Embedding { get; set; }
    public NpgsqlTsVector SearchVector { get; set; } = null!;
}
