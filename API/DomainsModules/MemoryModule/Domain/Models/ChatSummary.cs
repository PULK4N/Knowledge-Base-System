namespace MemoryModule.Domain.Models;

public sealed class ChatSummary
{
    public string Summary { get; set; } = string.Empty;
    public DateTime SummaryTimestamp { get; set; }
}
