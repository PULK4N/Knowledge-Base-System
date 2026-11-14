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

    /// <summary>
    /// Every summary added to the session, oldest first. <see cref="ChatSummary"/>
    /// keeps the latest one.
    /// </summary>
    public List<ChatSummary> ChatSummaries { get; set; } = [];

    public HashSet<MemoryRelatedEntity> RelatedEntities { get; set; } = [];
    public List<MemoryRelation> Relations { get; set; } = [];
}
