using EventSourcing.Shared.Models;

namespace FeatureModule.Domain.Models;

public readonly record struct MemoryHistoryRecord(
    string EventName,
    DateTime Timestamp,
    AggregateId AggregateId
);
