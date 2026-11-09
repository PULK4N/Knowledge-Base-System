using EventSourcing.Shared.Models;

namespace SettingsModule.Domain;

public sealed class SettingsStateData(AggregateId id) : ISharedStateData
{
    public AggregateId Id { get; init; } = id;
    public bool IsDeleted { get; set; }
    public bool IsInitialized { get; set; }
    public int CodeReviewBatchSize { get; set; }
    public string CodeReviewTaskMessage { get; set; } = string.Empty;
    public uint OrderNumber { get; set; }
}
