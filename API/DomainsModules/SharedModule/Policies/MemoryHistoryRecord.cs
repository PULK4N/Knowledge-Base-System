using EventSourcing.Shared.Models;

namespace SharedModule.Policies;

public readonly record struct MemoryHistoryRecord(
    string EventName,
    DateTime Timestamp,
    AggregateId AggregateId,
    PolicyId? PolicyId = null
);
