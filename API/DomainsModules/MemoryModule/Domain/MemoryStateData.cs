using System.Text.Json.Serialization;
using EventSourcing.Shared.Models;
using MemoryModule.Domain.Models;

namespace MemoryModule.Domain;

public sealed class MemoryStateData(AggregateId id) : ISharedStateData
{
    public AggregateId Id { get; init; } = id;
    public bool IsDeleted { get; set; }
    public ThreadId ThreadId { get; set; }
    public string SessionTitle { get; set; } = string.Empty;
    public Dictionary<PromptId, ChatPrompt> ChatPrompts { get; set; } = [];
    public ChatSummary ChatSummary { get; set; } = new();

    public HashSet<MemoryRelatedEntity> RelatedEntities { get; set; } = [];
    public List<MemoryRelation> Relations { get; set; } = [];

    [JsonIgnore]
    public bool HasSummary =>
        !string.IsNullOrWhiteSpace(ChatSummary.Summary)
        && ChatPrompts.Count > 0
        && ChatSummary.SummaryTimestamp
            > ChatPrompts.Values.Max(
                prompt => prompt.PromptStartTimestamp
            );
}
