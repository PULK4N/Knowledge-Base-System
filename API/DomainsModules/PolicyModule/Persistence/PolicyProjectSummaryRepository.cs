using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PolicyModule.Domain;
using PolicyModule.Persistence.Interfaces;
using PolicyModule.Persistence.Models;

namespace PolicyModule.Persistence;

public sealed class PolicyProjectSummaryRepository(IPolicyModuleDbContext dbContext)
    : IPolicyProjectSummaryRepository
{
    public async Task<List<PolicyProjectSummary>> List()
    {
        var summaries = await dbContext
            .PolicyProjectSummaries
            .AsNoTracking()
            .OrderBy(summary => summary.ProjectName)
            .ThenBy(summary => summary.ProjectAggregateId)
            .ToListAsync();

        return summaries
            .Select(
                summary =>
                    new PolicyProjectSummary(
                        summary.ProjectAggregateId,
                        summary.ProjectName,
                        JsonSerializer.Deserialize<List<string>>(summary.RepositoryPathsJson) ?? [ ]
                    )
            )
            .ToList();
    }

    public async Task<PolicyProjectSummary?> GetByName(
        string name,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedName = name.Trim().ToUpperInvariant();
        var summary = await dbContext.PolicyProjectSummaries
            .AsNoTracking()
            .Where(project => project.ProjectName.ToUpper() == normalizedName)
            .SingleOrDefaultAsync(cancellationToken);

        return summary is null ? null : ToReadModel(summary);
    }

    public async Task<PolicyProjectSummarySearchResult> Search(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default
    )
    {
        var query = dbContext.PolicyProjectSummaries.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLowerInvariant();
            query = query.Where(
                summary =>
                    summary.ProjectName.ToLower().Contains(normalizedSearch)
                    || summary.RepositoryPathsJson.ToLower().Contains(normalizedSearch)
            );
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var entries = await query
            .OrderBy(summary => summary.ProjectName)
            .ThenBy(summary => summary.ProjectAggregateId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var items = entries
            .Select(ToReadModel)
            .ToList();

        return new PolicyProjectSummarySearchResult(items, totalCount);
    }

    public async Task Write(List<ProjectPoliciesStateData> projects)
    {
        var context =
            dbContext as DbContext
            ?? throw new InvalidOperationException(
                $"{nameof(IPolicyModuleDbContext)} must be implemented by a {nameof(DbContext)}."
            );
        await using var transaction = context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync()
            : null;
        var projectIds = projects.Select(project => project.Id.Value).ToList();

        await dbContext
            .PolicyProjectSummaries
            .Where(summary => projectIds.Contains(summary.ProjectAggregateId))
            .ExecuteDeleteAsync();

        await dbContext
            .PolicyProjectSummaries
            .AddRangeAsync(
                projects
                    .Where(project => !project.IsDeleted)
                    .Select(
                        project =>
                            new PolicyProjectSummaryEntry
                            {
                                ProjectAggregateId = project.Id.Value,
                                ProjectName = project.ProjectName,
                                RepositoryPathsJson = JsonSerializer.Serialize(
                                    project.RepositoryPaths
                                )
                            }
                    )
            );
        await dbContext.SaveChangesAsync();

        if (transaction is not null)
            await transaction.CommitAsync();
    }

    private static PolicyProjectSummary ToReadModel(
        PolicyProjectSummaryEntry summary
    ) =>
        new(
            summary.ProjectAggregateId,
            summary.ProjectName,
            JsonSerializer.Deserialize<List<string>>(
                summary.RepositoryPathsJson
            ) ?? []
        );
}
