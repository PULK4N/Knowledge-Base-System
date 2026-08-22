using FeatureModule.Domain;
using FeatureModule.Persistence.Interfaces;
using FeatureModule.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace FeatureModule.Persistence;

public sealed class FeatureSummaryRepository(
    IFeatureModuleDbContext dbContext
) : IFeatureSummaryRepository
{
    public async Task<FeatureSummarySearchResult> Search(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default
    )
    {
        var query = dbContext.FeatureSummaries.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLowerInvariant();
            query = query.Where(
                feature =>
                    feature.Name.ToLower().Contains(normalizedSearch) ||
                    feature.Summary.ToLower().Contains(normalizedSearch)
            );
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(feature => feature.Name)
            .ThenBy(feature => feature.FeatureAggregateId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(
                feature => new FeatureSummary(
                    feature.FeatureAggregateId,
                    feature.ProjectId,
                    feature.Name,
                    feature.Summary,
                    feature.Status,
                    feature.CurrentPlanId,
                    feature.PlanCount,
                    feature.RecordCount
                )
            )
            .ToListAsync(cancellationToken);

        return new FeatureSummarySearchResult(items, totalCount);
    }

    public async Task Write(List<FeatureStateData> features)
    {
        var context = dbContext as DbContext
            ?? throw new InvalidOperationException(
                $"{nameof(IFeatureModuleDbContext)} must be implemented by a {nameof(DbContext)}."
            );
        await using var transaction = context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync()
            : null;
        var featureIds = features
            .Select(feature => feature.Id.Value)
            .ToList();

        await dbContext.FeatureSummaries
            .Where(
                summary => featureIds.Contains(
                    summary.FeatureAggregateId
                )
            )
            .ExecuteDeleteAsync();

        await dbContext.FeatureSummaries.AddRangeAsync(
            features
                .Where(feature => !feature.IsDeleted)
                .Select(
                    feature => new FeatureSummaryEntry
                    {
                        FeatureAggregateId = feature.Id.Value,
                        ProjectId = feature.ProjectId.Value,
                        Name = feature.Name,
                        Summary = feature.Summary,
                        Status = feature.Status,
                        CurrentPlanId = feature.CurrentPlanId?.Value,
                        PlanCount = feature.Plans.Count,
                        RecordCount = feature.Records.Count
                    }
                )
        );
        await dbContext.SaveChangesAsync();

        if (transaction is not null)
            await transaction.CommitAsync();
    }
}
