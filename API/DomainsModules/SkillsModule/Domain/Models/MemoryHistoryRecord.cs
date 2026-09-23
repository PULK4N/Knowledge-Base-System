using EventSourcing.Shared.Models;

namespace SkillsModule.Domain.Models;

public readonly record struct MemoryHistoryRecord(
    string EventName,
    DateTime Timestamp,
    AggregateId AggregateId
);
