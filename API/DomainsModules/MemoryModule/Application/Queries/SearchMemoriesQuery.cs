using ActionModule.Shared;
using ActionModule.Shared.Models;
using MemoryModule.Application.DTOs;
using MemoryModule.Persistence.Interfaces;

namespace MemoryModule.Application.Queries;

public sealed class SearchMemoriesQuery(
    IMemorySummaryRepository repository
) : Query<PagedResult<MemorySummaryDto>>
{
    public int Page { get; set; } = Pagination.DefaultPage;
    public int PageSize { get; set; } = Pagination.DefaultPageSize;
    public string? Search { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(Pagination.IsValid(Page, PageSize));

    protected override async Task<PagedResult<MemorySummaryDto>> ExecuteInternal(
        Executor executor
    )
    {
        var result = await repository.Search(Page, PageSize, Search);

        return new PagedResult<MemorySummaryDto>(
            result.Items
                .Select(MemorySummaryDto.FromReadModel)
                .ToList(),
            Page,
            PageSize,
            result.TotalCount
        );
    }
}
