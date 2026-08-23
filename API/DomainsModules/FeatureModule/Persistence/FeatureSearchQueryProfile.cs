using System.Linq.Expressions;
using ActionModule.Persistence;
using ActionModule.Shared.Models;
using FeatureModule.Contracts;
using FeatureModule.Persistence.Interfaces;
using FeatureModule.Persistence.Models;

namespace FeatureModule.Persistence;

internal sealed class FeatureSearchQueryProfile
    : IEntityQueryProfile<
        FeatureSearchEntry,
        FeatureSearchFilters,
        FeatureSearchSortField,
        FeatureSummary
    >
{
    public IQueryable<FeatureSearchEntry> ApplyFilters(
        IQueryable<FeatureSearchEntry> query,
        FeatureSearchFilters filters
    )
    {
        query = query.Where(feature => !feature.IsDeleted);

        if (filters.ProjectId is not null)
        {
            query = query.Where(
                feature => feature.ProjectId == filters.ProjectId
            );
        }

        return query;
    }

    public IQueryable<FeatureSearchEntry> ApplySearch(
        IQueryable<FeatureSearchEntry> query,
        string? search
    )
    {
        if (search is null)
            return query;

        var normalizedSearch = Normalize(search);
        return query.Where(
            feature => feature.SearchText.Contains(normalizedSearch)
        );
    }

    public IOrderedQueryable<FeatureSearchEntry> ApplySort(
        IQueryable<FeatureSearchEntry> query,
        SortRequest<FeatureSearchSortField> sort
    ) =>
        (sort.Field, sort.Direction) switch
        {
            (
                FeatureSearchSortField.Name,
                SortDirection.Ascending
            ) => query
                .OrderBy(feature => feature.NormalizedName)
                .ThenBy(feature => feature.Name)
                .ThenBy(feature => feature.FeatureAggregateId),
            (
                FeatureSearchSortField.Name,
                SortDirection.Descending
            ) => query
                .OrderByDescending(feature => feature.NormalizedName)
                .ThenByDescending(feature => feature.Name)
                .ThenByDescending(feature => feature.FeatureAggregateId),
            (
                FeatureSearchSortField.PlanCount,
                SortDirection.Ascending
            ) => query
                .OrderBy(feature => feature.PlanCount)
                .ThenBy(feature => feature.FeatureAggregateId),
            (
                FeatureSearchSortField.PlanCount,
                SortDirection.Descending
            ) => query
                .OrderByDescending(feature => feature.PlanCount)
                .ThenByDescending(feature => feature.FeatureAggregateId),
            (
                FeatureSearchSortField.RecordCount,
                SortDirection.Ascending
            ) => query
                .OrderBy(feature => feature.RecordCount)
                .ThenBy(feature => feature.FeatureAggregateId),
            (
                FeatureSearchSortField.RecordCount,
                SortDirection.Descending
            ) => query
                .OrderByDescending(feature => feature.RecordCount)
                .ThenByDescending(feature => feature.FeatureAggregateId),
            _ => throw new ArgumentOutOfRangeException(nameof(sort))
        };

    public Expression<Func<FeatureSearchEntry, FeatureSummary>> Projection =>
        feature => new FeatureSummary(
            feature.FeatureAggregateId,
            feature.ProjectId,
            feature.Name,
            feature.Summary,
            feature.Status,
            feature.CurrentPlanId,
            feature.PlanCount,
            feature.RecordCount
        );

    internal static string Normalize(string value) =>
        value.Trim().ToUpperInvariant();
}
