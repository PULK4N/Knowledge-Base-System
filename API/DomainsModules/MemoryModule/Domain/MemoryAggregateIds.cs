using EventSourcing.Shared.Models;

namespace MemoryModule.Domain;

public static class MemoryAggregateIds
{
    public static AggregateId SessionAggregateMap { get; } =
        AggregateId.FromDatabaseGuid(
            Guid.Parse("00000000-0000-0000-0000-000000000001")
        );
}
