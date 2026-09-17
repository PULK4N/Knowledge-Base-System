using EventSourcing.Shared.Models;
using Microsoft.EntityFrameworkCore;
using SharedModule.Persistence;

namespace MemoryModule.Persistence;

public sealed class MemoryEntityRelationRepository(
    IEntityRelationDbContext dbContext
)
{
    public const string ChangedEntity = "ChangedEntity";
    public const string ChangedInMemory = "ChangedInMemory";

    public async Task Write(
        List<AggregateId> memoryIds,
        List<EntityRelationWrite> relations,
        CancellationToken cancellationToken = default
    )
    {
        if (memoryIds.Count == 0)
            return;

        var context = dbContext as DbContext
            ?? throw new InvalidOperationException(
                $"{nameof(IEntityRelationDbContext)} must be implemented by a {nameof(DbContext)}."
            );
        var ids = memoryIds.Select(id => id.Value).ToList();
        await using var transaction = context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync(cancellationToken)
            : null;
        var ownedRelations = dbContext.EntityRelations.Where(relation =>
            (ids.Contains(relation.EntityId) && relation.RelationType == ChangedEntity)
            || (ids.Contains(relation.RelatedEntityId) && relation.RelationType == ChangedInMemory)
        );
        var existingSummaries = await ownedRelations
            .AsNoTracking()
            .Where(relation => relation.RelationType == ChangedEntity)
            .ToDictionaryAsync(
                relation => new { relation.EntityId, relation.RelatedEntityId },
                relation => relation.RelatedEntitySummary,
                cancellationToken
            );
        var rows = relations.SelectMany(relation => new List<EntityRelation>
        {
            new()
            {
                EntityId = relation.EntityId,
                RelatedEntityId = relation.RelatedEntityId,
                RelationType = ChangedEntity,
                RelatedEntitySummary = existingSummaries.GetValueOrDefault(
                    new { relation.EntityId, relation.RelatedEntityId },
                    relation.RelatedEntitySummary
                )
            },
            new()
            {
                EntityId = relation.RelatedEntityId,
                RelatedEntityId = relation.EntityId,
                RelationType = ChangedInMemory,
                RelatedEntitySummary = relation.EntitySummary
            }
        }).ToList();

        await ownedRelations.ExecuteDeleteAsync(cancellationToken);
        await dbContext.EntityRelations.AddRangeAsync(rows, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }
}
