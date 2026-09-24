using System.Linq.Expressions;
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
        ApplySort(filtered, request, profile)
            .Skip(request.Page.Offset)
            .Take(request.Page.Size)
            .Select(profile.Projection);

    private static IQueryable<TEntry> ApplySort<
        TEntry,
        TFilter,
        TSort,
        TResult
    >(
        IQueryable<TEntry> filtered,
        EntityQuery<TFilter, TSort> request,
        IEntityQueryProfile<TEntry, TFilter, TSort, TResult> profile
    ) where TSort : struct, Enum
    {
        var sorted = profile.ApplySort(filtered, request.Sort);
        if (
            request.NormalizedSearch is null
            || profile is not ISearchRankedEntityQueryProfile<TEntry> ranked
        )
            return sorted;

        var rankedSource = filtered.OrderByDescending(
            ranked.IsPrimarySearchMatch(request.NormalizedSearch)
        );
        var rewriter = new LeadingOrderingRewriter(
            filtered.Expression,
            rankedSource.Expression
        );
        var expression = rewriter.Visit(sorted.Expression);

        return rewriter.Rewritten
            ? sorted.Provider.CreateQuery<TEntry>(expression)
            : sorted;
    }

    /// <summary>
    /// Turns the profile's first OrderBy over the filtered query into a
    /// ThenBy over the search-ranked query, so the rank becomes the leading
    /// sort key while the profile keeps its full ordering and tie-breakers.
    /// </summary>
    private sealed class LeadingOrderingRewriter(
        Expression source,
        Expression rankedSource
    ) : ExpressionVisitor
    {
        public bool Rewritten { get; private set; }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (
                Rewritten
                || node.Method.DeclaringType != typeof(Queryable)
                || node.Arguments[0] != source
                || node.Method.Name is not (
                    nameof(Queryable.OrderBy)
                    or nameof(Queryable.OrderByDescending)
                )
            )
                return base.VisitMethodCall(node);

            var thenByName = node.Method.Name == nameof(Queryable.OrderBy)
                ? nameof(Queryable.ThenBy)
                : nameof(Queryable.ThenByDescending);
            var thenBy = typeof(Queryable)
                .GetMethods()
                .Single(
                    method =>
                        method.Name == thenByName
                        && method.GetParameters().Length
                            == node.Arguments.Count
                )
                .MakeGenericMethod(node.Method.GetGenericArguments());
            Rewritten = true;

            return Expression.Call(
                thenBy,
                node.Arguments.Skip(1).Prepend(rankedSource)
            );
        }
    }
}
