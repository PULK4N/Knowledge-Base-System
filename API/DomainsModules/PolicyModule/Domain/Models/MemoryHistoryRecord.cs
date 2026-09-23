using EventSourcing.Shared.Models;

namespace PolicyModule.Domain.Models;

public readonly record struct MemoryHistoryRecord(
    string EventName,
    DateTime Timestamp,
    AggregateId AggregateId,
    PolicyId? PolicyId = null
);
