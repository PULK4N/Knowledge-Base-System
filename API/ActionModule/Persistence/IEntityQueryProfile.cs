using System.Linq.Expressions;
using ActionModule.Shared.Models;

namespace ActionModule.Persistence;

public interface IEntityQueryProfile<
    TEntry,
    TFilter,
    TSort,
    TResult
> where TSort : struct, Enum
{
    IQueryable<TEntry> ApplyFilters(
        IQueryable<TEntry> query,
        TFilter filters
    );

    IQueryable<TEntry> ApplySearch(
        IQueryable<TEntry> query,
        string? search
    );

    /// <summary>
    /// Applies the requested ordering and a final unique tie-breaker so
    /// pagination remains deterministic.
    /// </summary>
    IOrderedQueryable<TEntry> ApplySort(
        IQueryable<TEntry> query,
        SortRequest<TSort> sort
    );

    Expression<Func<TEntry, TResult>> Projection { get; }
}
