using EmbeddingModule;
using EventSourcing.Persistence;
using FeatureModule.Persistence.Interfaces;

namespace PostgreSqlModule;

internal sealed class PostgreSqlFeatureSearchProjectionWriter(
    EventSourcingDbContext dbContext,
    IFeatureResearchSearchRepository researchRepository,
    IKnowledgeSearchRepository knowledgeRepository
) : IFeatureSearchProjectionWriter
{
    public async Task Write(
        FeatureSearchProjectionBatch batch,
        CancellationToken cancellationToken = default
    )
    {
        await using var transaction = dbContext.Database.CurrentTransaction is null
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        await researchRepository.Write(
            batch.FeatureAggregateIds,
            batch.ResearchDocuments,
            cancellationToken
        );
        await knowledgeRepository.Write(
            KnowledgeSearchOwnerTypes.Feature,
            batch.FeatureAggregateIds,
            batch.KnowledgeDocuments,
            cancellationToken
        );

        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }
}
