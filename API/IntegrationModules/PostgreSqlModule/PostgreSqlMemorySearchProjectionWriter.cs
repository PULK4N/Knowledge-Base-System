using EmbeddingModule;
using EventSourcing.Persistence;
using MemoryModule.Persistence.Interfaces;

namespace PostgreSqlModule;

internal sealed class PostgreSqlMemorySearchProjectionWriter(
    EventSourcingDbContext dbContext,
    IMemorySearchRepository memoryRepository,
    IKnowledgeSearchRepository knowledgeRepository
) : IMemorySearchProjectionWriter
{
    public async Task Write(
        MemorySearchProjectionBatch batch,
        CancellationToken cancellationToken = default
    )
    {
        await using var transaction = dbContext.Database.CurrentTransaction is null
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        await memoryRepository.Write(
            batch.MemoryAggregateIds,
            batch.MemoryDocuments,
            cancellationToken
        );
        await knowledgeRepository.Write(
            KnowledgeSearchOwnerTypes.Memory,
            batch.MemoryAggregateIds,
            batch.KnowledgeDocuments,
            cancellationToken
        );

        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }
}
