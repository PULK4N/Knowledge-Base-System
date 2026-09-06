using EmbeddingModule;
using EventSourcing.Persistence;
using SkillsModule.Persistence.Interfaces;

namespace PostgreSqlModule;

internal sealed class PostgreSqlSkillSearchProjectionWriter(
    EventSourcingDbContext dbContext,
    ISkillSearchRepository skillRepository,
    IKnowledgeSearchRepository knowledgeRepository
) : ISkillSearchProjectionWriter
{
    public async Task Write(
        SkillSearchProjectionBatch batch,
        CancellationToken cancellationToken = default
    )
    {
        await using var transaction = dbContext.Database.CurrentTransaction is null
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        await skillRepository.Write(
            batch.SkillAggregateIds,
            batch.SkillDocuments,
            cancellationToken
        );
        await knowledgeRepository.Write(
            KnowledgeSearchOwnerTypes.Skill,
            batch.SkillAggregateIds,
            batch.KnowledgeDocuments,
            cancellationToken
        );

        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }
}
