using System.ComponentModel.DataAnnotations;
using ActionModule.Shared.Models;
using MemoryModule.Persistence.Interfaces;

namespace MemoryModule.API.Requests;

public abstract record MemorySummarySearchRequest : PagedSearchRequest
{
    public bool? HasSummary { get; init; }

    [Range(1, int.MaxValue)]
    public int? MinimumPromptCount { get; init; }

    [EnumDataType(typeof(MemorySummarySortField))]
    public MemorySummarySortField SortBy { get; init; }
}

public sealed record ListMemoriesRequest : MemorySummarySearchRequest
{
    public ListMemoriesRequest()
    {
        SortBy = MemorySummarySortField.LastActivity;
        SortDirection = SortDirection.Descending;
    }
}

public sealed record HybridSearchMemoriesRequest : MemorySummarySearchRequest
{
    [Required]
    [StringLength(EntityQueryLimits.MaximumSearchLength)]
    public string Query { get; init; } = string.Empty;

    public HybridSearchMemoriesRequest()
    {
        SortBy = MemorySummarySortField.Relevance;
        SortDirection = SortDirection.Descending;
    }
}
