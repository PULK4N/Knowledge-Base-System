using Microsoft.EntityFrameworkCore;

namespace SharedModule.Persistence;

public interface IEntityRelationRepository
{
    Task Add(
        EntityRelationWrite relation,
        CancellationToken cancellationToken = default
    );
}

public sealed class EntityRelationRepository(
    IEntityRelationDbContext dbContext
) : IEntityRelationRepository
{
    public async Task Add(
        EntityRelationWrite relation,
        CancellationToken cancellationToken = default
    )
    {
        var context = dbContext as DbContext
            ?? throw new InvalidOperationException(
                $"{nameof(IEntityRelationDbContext)} must be implemented by a {nameof(DbContext)}."
            );
        await using var transaction = context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync(cancellationToken)
            : null;

        await dbContext.EntityRelations
            .Where(
                entry =>
                    (
                        entry.EntityId == relation.EntityId
                        && entry.RelatedEntityId == relation.RelatedEntityId
                    )
                    || (
                        entry.EntityId == relation.RelatedEntityId
                        && entry.RelatedEntityId == relation.EntityId
                    )
            )
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.EntityRelations.AddRangeAsync(
            [
                new EntityRelation
                {
                    EntityId = relation.EntityId,
                    RelatedEntityId = relation.RelatedEntityId,
                    RelationType = relation.RelationType,
                    RelatedEntitySummary = relation.RelatedEntitySummary
                },
                new EntityRelation
                {
                    EntityId = relation.RelatedEntityId,
                    RelatedEntityId = relation.EntityId,
                    RelationType = relation.ReverseRelationType,
                    RelatedEntitySummary = relation.EntitySummary
                }
            ],
            cancellationToken
        );
        await dbContext.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }
}
