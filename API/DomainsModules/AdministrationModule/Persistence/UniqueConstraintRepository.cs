using AdministrationModule.Application.Persistence.UniqueConstraints;
using EventSourcing.Persistence;
using EventSourcing.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AdministrationModule.Persistence;

public sealed class UniqueConstraintRepository(
    EventSourcingDbContext dbContext
) : IUniqueConstraintRepository
{
    public async Task<List<AggregateId>> GetAggregateIds(string stateMachineId)
    {
        var aggregateGuids = await dbContext.SerializedEventPayload
            .AsNoTracking()
            .Where(payload => payload.StateMachineId == stateMachineId)
            .Select(payload => payload.AggregateId)
            .Distinct()
            .OrderBy(aggregateId => aggregateId)
            .ToListAsync();

        return aggregateGuids.Select(AggregateId.FromDatabaseGuid).ToList();
    }

    public async Task Write(UniqueConstraintRebuildBatch batch)
    {
        var aggregateGuids = batch.AggregateIds
            .Select(aggregateId => aggregateId.Value)
            .ToList();

        await using var transaction = dbContext.Database.CurrentTransaction is null
            ? await dbContext.Database.BeginTransactionAsync()
            : null;

        await dbContext.UniqueConstraints
            .Where(constraint => aggregateGuids.Contains(constraint.AggregateId))
            .ExecuteDeleteAsync();
        await dbContext.UniqueConstraints.AddRangeAsync(batch.Constraints);
        await dbContext.SaveChangesAsync();

        if (transaction is not null)
            await transaction.CommitAsync();
    }
}
