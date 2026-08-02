using EventSourcing.Shared.Models;
using MemoryModule.Domain.Models;

namespace MemoryModule.Domain;

public sealed class MemoryStateData(AggregateId id) : ISharedStateData
{
    public AggregateId Id { get; init; } = id;
    public bool IsDeleted { get; set; }
    public ThreadId ThreadId { get; set; }
    public Dictionary<PromptId, ChatPrompt> ChatPrompts { get; set; } = [];
}
