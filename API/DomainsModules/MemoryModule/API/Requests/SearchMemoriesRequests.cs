using System.ComponentModel.DataAnnotations;
using ActionModule.Shared.Models;
using EmbeddingModule;
using MemoryModule.Application.Queries;
using MemoryModule.Persistence.Interfaces;

namespace MemoryModule.API.Requests;

public abstract record MemorySummarySearchRequest : PagedSearchRequest
{
    public bool? HasSummary { get; init; }

    [Range(1, int.MaxValue)]
    public int? MinimumPromptCount { get; init; }

    public DateTime? ActiveFrom { get; init; }
    public DateTime? ActiveTo { get; init; }

    [StringLength(MemoryModelFilter.MaximumLength)]
    public string? Model { get; init; }

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

/// <summary>
/// Query drives the semantic vector leg and Keywords the full-text leg, where
/// a chunk must contain every keyword. Either may be empty, and that leg is
/// then skipped; the query is rejected when both are empty.
/// </summary>
public sealed record HybridSearchMemoriesRequest : MemorySummarySearchRequest
{
    [StringLength(EntityQueryLimits.MaximumSearchLength)]
    public string? Query { get; init; }

    [MaxLength(SearchKeywordLimits.MaximumCount)]
    public List<string> Keywords { get; init; } = [];

    public HybridSearchMemoriesRequest()
    {
        SortBy = MemorySummarySortField.Relevance;
        SortDirection = SortDirection.Descending;
    }
}

public sealed record SearchMemoryToolCallsRequest : PagedSearchRequest
{
    [StringLength(EntityQueryLimits.MaximumSearchLength)]
    public string? ToolName { get; init; }

    public Guid? MemoryId { get; init; }
    public Guid? ThreadId { get; init; }
    public Guid? PromptId { get; init; }

    [MaxLength(SearchKeywordLimits.MaximumCount)]
    public List<string> InputKeywords { get; init; } = [];

    [MaxLength(SearchKeywordLimits.MaximumCount)]
    public List<string> OutputKeywords { get; init; } = [];

    [EnumDataType(typeof(MemoryToolCallSortField))]
    public MemoryToolCallSortField SortBy { get; init; }

    public SearchMemoryToolCallsRequest()
    {
        SortBy = MemoryToolCallSortField.Timestamp;
        SortDirection = SortDirection.Descending;
    }
}
