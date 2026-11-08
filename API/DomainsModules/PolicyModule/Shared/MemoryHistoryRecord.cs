using EventSourcing.Shared.Models;

namespace PolicyModule.Shared;

public readonly record struct MemoryHistoryRecord(
    string EventName,
    DateTime Timestamp,
    AggregateId AggregateId,
    PolicyId? PolicyId = null
);
