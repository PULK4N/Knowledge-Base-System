using EventSourcing.Shared.Models;
using SharedModule.Constants;

namespace MemoryModule.Domain;

public static class MemoryAggregateIds
{
    public static AggregateId SessionAggregateMap { get; } =
        AggregateId.FromDatabaseGuid(
            StateDataAggregateIds.SessionAggregateMap
        );
}
