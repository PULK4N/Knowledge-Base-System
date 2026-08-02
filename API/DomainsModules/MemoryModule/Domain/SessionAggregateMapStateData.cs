using EventSourcing.Shared.Models;
using MemoryModule.Domain.Models;
using SharedModule.Constants;

namespace MemoryModule.Domain;

public sealed class SessionAggregateMapStateData : ISharedStateData
{
    public SessionAggregateMapStateData(AggregateId aggregateId)
    {
        _ = aggregateId;
    }

    public AggregateId Id { get; init; } =
        AggregateId.FromDatabaseGuid(
            StateDataAggregateIds.SessionAggregateMap
        );
    public bool IsDeleted { get; set; }
    public Dictionary<ThreadId, AggregateId> AggregateIdsBySession { get; set; } = [ ];
}
