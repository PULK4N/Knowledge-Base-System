using System.Data;
using ActionModule.Persistence;
using ActionModule.Shared.Models;
using Microsoft.EntityFrameworkCore;
using SkillsModule.Contracts;
using SkillsModule.Domain;
using SkillsModule.Persistence.Interfaces;
using SkillsModule.Persistence.Models;

namespace SkillsModule.Persistence;

public sealed class SkillListRepository(
    ISkillsModuleDbContext dbContext
) : ISkillListRepository
{
    private static readonly SkillListQueryProfile QueryProfile = new();

    public Task<PagedResult<SkillListItem>> Search(
        EntityQuery<SkillSearchFilters, SkillSearchSortField> request,
        CancellationToken cancellationToken = default
    ) =>
        EntityQueryExecutor.Execute(
            dbContext.SkillListEntries,
            request,
            QueryProfile,
            cancellationToken
        );

    public IQueryable<SkillListItem> CreatePageQuery(
        EntityQuery<SkillSearchFilters, SkillSearchSortField> request
    ) =>
        EntityQueryExecutor.BuildPageQuery(
            dbContext.SkillListEntries,
            request,
            QueryProfile
        );

    internal async Task Write(
        List<SkillListUpdate> updates,
        CancellationToken cancellationToken = default
    )
    {
        if (updates.Count == 0)
            return;

        var context = dbContext as DbContext
            ?? throw new InvalidOperationException(
                $"{nameof(ISkillsModuleDbContext)} must be implemented by a {nameof(DbContext)}."
            );
        await using var transaction = context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken
            )
            : null;
        var latestUpdates = updates
            .GroupBy(update => update.State.Id.Value)
            .Select(
                group => group
                    .OrderByDescending(update => update.OrderNumber)
                    .First()
            )
            .ToList();

        foreach (var update in latestUpdates)
        {
            var entry = ToEntry(update);
            var current = await dbContext.SkillListEntries
                .SingleOrDefaultAsync(
                    skill =>
                        skill.SkillAggregateId
                            == entry.SkillAggregateId,
                    cancellationToken
                );

            if (current is null)
            {
                await dbContext.SkillListEntries.AddAsync(
                    entry,
                    cancellationToken
                );
                continue;
            }

            if (current.ProjectedOrderNumber > entry.ProjectedOrderNumber)
                continue;

            current.Name = entry.Name;
            current.NormalizedName = entry.NormalizedName;
            current.Description = entry.Description;
            current.SearchText = entry.SearchText;
            current.IsDeleted = entry.IsDeleted;
            current.ReferenceCount = entry.ReferenceCount;
            current.AttachmentCount = entry.AttachmentCount;
            current.ProjectedOrderNumber = entry.ProjectedOrderNumber;

            await dbContext.SkillListTags
                .Where(tag => tag.SkillListEntryId == current.Id)
                .ExecuteDeleteAsync(cancellationToken);
            foreach (var tag in entry.Tags)
                tag.SkillListEntryId = current.Id;
            await dbContext.SkillListTags.AddRangeAsync(
                entry.Tags,
                cancellationToken
            );
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);

        foreach (
            var trackedEntry in context.ChangeTracker
                .Entries<SkillListEntry>()
                .ToList()
        )
        {
            trackedEntry.State = EntityState.Detached;
        }
        foreach (
            var trackedTag in context.ChangeTracker
                .Entries<SkillListTagEntry>()
                .ToList()
        )
        {
            trackedTag.State = EntityState.Detached;
        }
    }

    private static SkillListEntry ToEntry(SkillListUpdate update)
    {
        var skill = update.State;
        var tags = skill.IsDeleted
            ? []
            : skill.Tags
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .GroupBy(
                    SkillListQueryProfile.Normalize,
                    StringComparer.Ordinal
                )
                .Select(
                    group => new SkillListTagEntry
                    {
                        Tag = group.First().Trim(),
                        NormalizedTag = group.Key
                    }
                )
                .OrderBy(tag => tag.NormalizedTag, StringComparer.Ordinal)
                .ThenBy(tag => tag.Tag, StringComparer.Ordinal)
                .ToList();
        var normalizedName = SkillListQueryProfile.Normalize(skill.Name);
        var normalizedDescription = SkillListQueryProfile.Normalize(
            skill.Description
        );
        var normalizedTags = string.Join(
            '\n',
            tags.Select(tag => tag.NormalizedTag)
        );

        return new SkillListEntry
        {
            SkillAggregateId = skill.Id.Value,
            Name = skill.Name,
            NormalizedName = normalizedName,
            Description = skill.Description,
            SearchText = $"{normalizedName}\n{normalizedDescription}\n{normalizedTags}",
            IsDeleted = skill.IsDeleted,
            ReferenceCount = skill.References.Count,
            AttachmentCount = skill.Attachments.Count,
            ProjectedOrderNumber = update.OrderNumber,
            Tags = tags
        };
    }
}

internal sealed record SkillListUpdate(
    SkillStateData State,
    long OrderNumber
);
