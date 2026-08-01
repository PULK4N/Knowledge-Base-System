using EventSourcing.Shared.Models;
using MemoryModule.Domain.Models;

namespace MemoryModule.Domain;

public sealed class SessionAggregateMapStateData(
    AggregateId id
) : ISharedStateData
{
    public AggregateId Id { get; init; } = id;
    public bool IsDeleted { get; set; }
    public Dictionary<ThreadId, AggregateId> AggregateIdsBySession { get; set; } = [];
}
