using Microsoft.EntityFrameworkCore;

namespace SharedModule.Persistence;

public interface IEntityRelationDbContext
{
    DbSet<EntityRelation> EntityRelations { get; }

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default
    );
}
