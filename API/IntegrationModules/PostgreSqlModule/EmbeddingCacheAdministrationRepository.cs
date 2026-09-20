using AdministrationModule.Application.Persistence;
using EventSourcing.Persistence;
using Microsoft.EntityFrameworkCore;

namespace PostgreSqlModule;

internal sealed class EmbeddingCacheAdministrationRepository(
    EventSourcingDbContext dbContext
) : IEmbeddingCacheAdministrationRepository
{
    public Task<int> Clear(CancellationToken cancellationToken = default) =>
        dbContext.Set<EmbeddingCacheEntry>().ExecuteDeleteAsync(
            cancellationToken
        );
}
