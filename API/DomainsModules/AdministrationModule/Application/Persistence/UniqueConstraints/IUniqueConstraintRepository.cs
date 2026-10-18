using EventSourcing.Shared.Models;

namespace AdministrationModule.Application.Persistence.UniqueConstraints;

public interface IUniqueConstraintRepository
{
    Task<List<AggregateId>> GetAggregateIds(string stateMachineId);

    Task Write(UniqueConstraintRebuildBatch batch);
}
