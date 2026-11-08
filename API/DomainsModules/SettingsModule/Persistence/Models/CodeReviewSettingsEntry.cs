namespace SettingsModule.Persistence.Models;

public sealed class CodeReviewSettingsEntry
{
    public Guid AggregateId { get; set; }
    public int BatchSize { get; set; }
    public string TaskMessage { get; set; } = string.Empty;
    public uint OrderNumber { get; set; }
}
