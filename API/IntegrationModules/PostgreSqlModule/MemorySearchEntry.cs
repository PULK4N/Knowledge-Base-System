using NpgsqlTypes;
using Pgvector;

namespace PostgreSqlModule;

internal sealed class MemorySearchEntry
{
    public Guid MemoryAggregateId { get; set; }
    public Guid ThreadId { get; set; }
    public Guid PromptId { get; set; }
    public int HookIndex { get; set; }
    public int ChunkIndex { get; set; }
    public DateTime PromptStartTimestamp { get; set; }
    public required string HookEventName { get; set; }
    public required string Text { get; set; }
    public required Vector Embedding { get; set; }
    public NpgsqlTsVector SearchVector { get; set; } = null!;
}
