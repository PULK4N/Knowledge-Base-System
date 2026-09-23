using EventSourcing.Shared.Models;

namespace MemoryModule.Domain.Models;

public readonly record struct MemoryRelation(
    string Relation,
    AggregateId AggregateId
);
