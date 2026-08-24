using EmbeddingModule;
using EventSourcing.Persistence;

namespace PostgreSqlModule;

internal sealed class PostgreSqlKnowledgeSearchProjectionTransaction(
    EventSourcingDbContext dbContext
) : IKnowledgeSearchProjectionTransaction
{
    public async Task Execute(
        Func<Task> writes,
        CancellationToken cancellationToken = default
    )
    {
        if (dbContext.Database.CurrentTransaction is not null)
        {
            await writes();
            return;
        }

        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken);
        await writes();
        await transaction.CommitAsync(cancellationToken);
    }
}
