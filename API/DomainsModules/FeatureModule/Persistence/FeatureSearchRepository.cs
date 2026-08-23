using System.Data;
using ActionModule.Persistence;
using ActionModule.Shared.Models;
using FeatureModule.Contracts;
using FeatureModule.Domain;
using FeatureModule.Persistence.Interfaces;
using FeatureModule.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace FeatureModule.Persistence;

public sealed class FeatureSearchRepository(
    IFeatureModuleDbContext dbContext
) : IFeatureSearchRepository
{
    private static readonly FeatureSearchQueryProfile QueryProfile = new();

    public Task<PagedResult<FeatureSummary>> Search(
        EntityQuery<FeatureSearchFilters, FeatureSearchSortField> request,
        CancellationToken cancellationToken = default
    ) =>
        EntityQueryExecutor.Execute(
            dbContext.FeatureSearchEntries,
            request,
            QueryProfile,
            cancellationToken
        );

    public IQueryable<FeatureSummary> CreatePageQuery(
        EntityQuery<FeatureSearchFilters, FeatureSearchSortField> request
    ) =>
        EntityQueryExecutor.BuildPageQuery(
            dbContext.FeatureSearchEntries,
            request,
            QueryProfile
        );

    internal async Task Write(
        List<FeatureSearchUpdate> updates,
        CancellationToken cancellationToken = default
    )
    {
        if (updates.Count == 0)
            return;

        var context = dbContext as DbContext
            ?? throw new InvalidOperationException(
                $"{nameof(IFeatureModuleDbContext)} must be implemented by a {nameof(DbContext)}."
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
            var updated = await UpdateExisting(
                entry,
                cancellationToken
            );
            if (updated)
                continue;

            var exists = await dbContext.FeatureSearchEntries
                .AsNoTracking()
                .AnyAsync(
                    current =>
                        current.FeatureAggregateId
                            == entry.FeatureAggregateId,
                    cancellationToken
                );
            if (!exists)
            {
                await dbContext.FeatureSearchEntries.AddAsync(
                    entry,
                    cancellationToken
                );
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);

        foreach (
            var trackedEntry in context.ChangeTracker
                .Entries<FeatureSearchEntry>()
                .ToList()
        )
        {
            trackedEntry.State = EntityState.Detached;
        }
    }

    private static FeatureSearchEntry ToEntry(FeatureSearchUpdate update)
    {
        var feature = update.State;
        var normalizedName = FeatureSearchQueryProfile.Normalize(
            feature.Name
        );
        var normalizedSummary = FeatureSearchQueryProfile.Normalize(
            feature.Summary
        );

        return new FeatureSearchEntry
        {
            FeatureAggregateId = feature.Id.Value,
            ProjectId = feature.ProjectId.Value,
            Name = feature.Name,
            NormalizedName = normalizedName,
            Summary = feature.Summary,
            SearchText = $"{normalizedName}\n{normalizedSummary}",
            Status = feature.Status,
            IsDeleted = feature.IsDeleted,
            CurrentPlanId = feature.CurrentPlanId?.Value,
            PlanCount = feature.Plans.Count,
            RecordCount = feature.Records.Count,
            ProjectedOrderNumber = update.OrderNumber
        };
    }

    private async Task<bool> UpdateExisting(
        FeatureSearchEntry entry,
        CancellationToken cancellationToken
    ) =>
        await dbContext.FeatureSearchEntries
            .Where(
                current =>
                    current.FeatureAggregateId == entry.FeatureAggregateId
                    && current.ProjectedOrderNumber
                        <= entry.ProjectedOrderNumber
            )
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        current => current.ProjectId,
                        entry.ProjectId
                    )
                    .SetProperty(current => current.Name, entry.Name)
                    .SetProperty(
                        current => current.NormalizedName,
                        entry.NormalizedName
                    )
                    .SetProperty(current => current.Summary, entry.Summary)
                    .SetProperty(
                        current => current.SearchText,
                        entry.SearchText
                    )
                    .SetProperty(current => current.Status, entry.Status)
                    .SetProperty(
                        current => current.IsDeleted,
                        entry.IsDeleted
                    )
                    .SetProperty(
                        current => current.CurrentPlanId,
                        entry.CurrentPlanId
                    )
                    .SetProperty(
                        current => current.PlanCount,
                        entry.PlanCount
                    )
                    .SetProperty(
                        current => current.RecordCount,
                        entry.RecordCount
                    )
                    .SetProperty(
                        current => current.ProjectedOrderNumber,
                        entry.ProjectedOrderNumber
                    ),
                cancellationToken
            ) > 0;
}

internal sealed record FeatureSearchUpdate(
    FeatureStateData State,
    long OrderNumber
);
