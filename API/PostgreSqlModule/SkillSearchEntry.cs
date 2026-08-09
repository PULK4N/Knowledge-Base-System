using NpgsqlTypes;
using Pgvector;

namespace PostgreSqlModule;

internal sealed class SkillSearchEntry
{
    public Guid SkillAggregateId { get; set; }
    public required string SkillName { get; set; }
    public required string SourcePath { get; set; }
    public int ChunkIndex { get; set; }
    public required string Text { get; set; }
    public required Vector Embedding { get; set; }
    public NpgsqlTsVector SearchVector { get; set; } = null!;
}
