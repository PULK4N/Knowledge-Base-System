using System.Linq.Expressions;
using ActionModule.Persistence;
using ActionModule.Shared.Models;
using SkillsModule.Contracts;
using SkillsModule.Persistence.Interfaces;
using SkillsModule.Persistence.Models;

namespace SkillsModule.Persistence;

internal sealed class SkillListQueryProfile
    : IEntityQueryProfile<
        SkillListEntry,
        SkillSearchFilters,
        SkillSearchSortField,
        SkillListItem
    >
{
    public IQueryable<SkillListEntry> ApplyFilters(
        IQueryable<SkillListEntry> query,
        SkillSearchFilters filters
    )
    {
        query = query.Where(skill => !skill.IsDeleted);

        if (!string.IsNullOrWhiteSpace(filters.Tag))
        {
            var normalizedTag = Normalize(filters.Tag);
            query = query.Where(
                skill => skill.Tags.Any(
                    tag => tag.NormalizedTag == normalizedTag
                )
            );
        }

        if (filters.HasReferences is not null)
        {
            query = filters.HasReferences.Value
                ? query.Where(skill => skill.ReferenceCount > 0)
                : query.Where(skill => skill.ReferenceCount == 0);
        }

        if (filters.HasAttachments is not null)
        {
            query = filters.HasAttachments.Value
                ? query.Where(skill => skill.AttachmentCount > 0)
                : query.Where(skill => skill.AttachmentCount == 0);
        }

        return query;
    }

    public IQueryable<SkillListEntry> ApplySearch(
        IQueryable<SkillListEntry> query,
        string? search
    )
    {
        if (search is null)
            return query;

        var normalizedSearch = Normalize(search);
        return query.Where(
            skill => skill.SearchText.Contains(normalizedSearch)
        );
    }

    public IOrderedQueryable<SkillListEntry> ApplySort(
        IQueryable<SkillListEntry> query,
        SortRequest<SkillSearchSortField> sort
    ) =>
        (sort.Field, sort.Direction) switch
        {
            (
                SkillSearchSortField.Name,
                SortDirection.Ascending
            ) => query
                .OrderBy(skill => skill.NormalizedName)
                .ThenBy(skill => skill.Name)
                .ThenBy(skill => skill.SkillAggregateId),
            (
                SkillSearchSortField.Name,
                SortDirection.Descending
            ) => query
                .OrderByDescending(skill => skill.NormalizedName)
                .ThenByDescending(skill => skill.Name)
                .ThenByDescending(skill => skill.SkillAggregateId),
            (
                SkillSearchSortField.ReferenceCount,
                SortDirection.Ascending
            ) => query
                .OrderBy(skill => skill.ReferenceCount)
                .ThenBy(skill => skill.SkillAggregateId),
            (
                SkillSearchSortField.ReferenceCount,
                SortDirection.Descending
            ) => query
                .OrderByDescending(skill => skill.ReferenceCount)
                .ThenByDescending(skill => skill.SkillAggregateId),
            (
                SkillSearchSortField.AttachmentCount,
                SortDirection.Ascending
            ) => query
                .OrderBy(skill => skill.AttachmentCount)
                .ThenBy(skill => skill.SkillAggregateId),
            (
                SkillSearchSortField.AttachmentCount,
                SortDirection.Descending
            ) => query
                .OrderByDescending(skill => skill.AttachmentCount)
                .ThenByDescending(skill => skill.SkillAggregateId),
            _ => throw new ArgumentOutOfRangeException(nameof(sort))
        };

    public Expression<Func<SkillListEntry, SkillListItem>> Projection =>
        skill => new SkillListItem(
            skill.SkillAggregateId,
            skill.Name,
            skill.Description,
            skill.Tags
                .OrderBy(tag => tag.NormalizedTag)
                .ThenBy(tag => tag.Tag)
                .Select(tag => tag.Tag)
                .ToList(),
            skill.ReferenceCount,
            skill.AttachmentCount
        );

    internal static string Normalize(string value) =>
        value.Trim().ToUpperInvariant();
}
