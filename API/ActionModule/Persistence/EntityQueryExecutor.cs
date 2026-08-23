using ActionModule.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace ActionModule.Persistence;

public static class EntityQueryExecutor
{
    public static IQueryable<TResult> BuildPageQuery<
        TEntry,
        TFilter,
        TSort,
        TResult
    >(
        IQueryable<TEntry> source,
        EntityQuery<TFilter, TSort> request,
        IEntityQueryProfile<TEntry, TFilter, TSort, TResult> profile
    ) where TEntry : class
        where TSort : struct, Enum
    {
        var filtered = BuildFilteredQuery(source, request, profile);

        return ApplyPage(filtered, request, profile);
    }

    public static async Task<PagedResult<TResult>> Execute<
        TEntry,
        TFilter,
        TSort,
        TResult
    >(
        IQueryable<TEntry> source,
        EntityQuery<TFilter, TSort> request,
        IEntityQueryProfile<TEntry, TFilter, TSort, TResult> profile,
        CancellationToken cancellationToken = default
    ) where TEntry : class
        where TSort : struct, Enum
    {
        var filtered = BuildFilteredQuery(source, request, profile);

        var totalCount = await filtered.CountAsync(cancellationToken);
        var items = await ApplyPage(filtered, request, profile)
            .ToListAsync(cancellationToken);

        return new PagedResult<TResult>(
            items,
            request.Page.Number,
            request.Page.Size,
            totalCount
        );
    }

    private static IQueryable<TEntry> BuildFilteredQuery<
        TEntry,
        TFilter,
        TSort,
        TResult
    >(
        IQueryable<TEntry> source,
        EntityQuery<TFilter, TSort> request,
        IEntityQueryProfile<TEntry, TFilter, TSort, TResult> profile
    ) where TEntry : class
        where TSort : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(profile);

        if (!request.IsValid)
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "The requested query criteria are invalid."
            );

        var filtered = profile.ApplyFilters(
            source.AsNoTracking(),
            request.Filters
        );
        return profile.ApplySearch(
            filtered,
            request.NormalizedSearch
        );
    }

    private static IQueryable<TResult> ApplyPage<
        TEntry,
        TFilter,
        TSort,
        TResult
    >(
        IQueryable<TEntry> filtered,
        EntityQuery<TFilter, TSort> request,
        IEntityQueryProfile<TEntry, TFilter, TSort, TResult> profile
    ) where TSort : struct, Enum =>
        profile.ApplySort(filtered, request.Sort)
            .Skip(request.Page.Offset)
            .Take(request.Page.Size)
            .Select(profile.Projection);
}
