using ActionModule.Shared.Models;

namespace ActionModule.Shared;

public abstract class PagedQuery<TItem> : Query<PagedResult<TItem>>
{
    public int Page { get; set; } = Pagination.DefaultPage;
    public int PageSize { get; set; } = Pagination.DefaultPageSize;
    public string? Search { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            Pagination.IsValid(Page, PageSize)
            && (Search?.Length ?? 0)
                <= EntityQueryLimits.MaximumSearchLength
        );

    protected EntityQuery<TFilter, TSort> CreateEntityQuery<
        TFilter,
        TSort
    >(
        TFilter filters,
        TSort sortBy,
        SortDirection sortDirection
    ) where TSort : struct, Enum =>
        new(
            new PageRequest(Page, PageSize),
            Search,
            filters,
            new SortRequest<TSort>(sortBy, sortDirection)
        );
}
