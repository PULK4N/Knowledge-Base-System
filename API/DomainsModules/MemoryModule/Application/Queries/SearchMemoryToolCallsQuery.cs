using ActionModule.Shared;
using ActionModule.Shared.Models;
using MemoryModule.Application.DTOs;
using MemoryModule.Persistence.Interfaces;

namespace MemoryModule.Application.Queries;

/// <summary>
/// Lists recorded tool calls across every memory. Search matches call content,
/// while the tool-name filter narrows the results by name.
/// </summary>
public sealed class SearchMemoryToolCallsQuery(
    IMemoryToolCallRepository repository
) : PagedQuery<MemoryToolCallSearchItemDto>
{
    public MemoryToolCallSortField SortBy { get; set; } =
        MemoryToolCallSortField.Timestamp;
    public string? ToolName { get; set; }
    public SortDirection SortDirection { get; set; } =
        SortDirection.Descending;

    public override async Task<bool> CanExecute(Executor executor) =>
        await base.CanExecute(executor)
        && Enum.IsDefined(SortBy)
        && Enum.IsDefined(SortDirection)
        && (ToolName?.Length ?? 0) <= EntityQueryLimits.MaximumSearchLength;

    protected override async Task<
        PagedResult<MemoryToolCallSearchItemDto>
    > ExecuteInternal(Executor executor)
    {
        var result = await repository.Search(
            CreateEntityQuery(
                new MemoryToolCallFilters(
                    string.IsNullOrWhiteSpace(ToolName) ? null : ToolName.Trim()
                ),
                SortBy,
                SortDirection
            )
        );

        return result.Map(MemoryToolCallSearchItemDto.FromReadModel);
    }
}
