using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PolicyModule.Domain;
using PolicyModule.Persistence.Interfaces;
using PolicyModule.Persistence.Models;

namespace PolicyModule.Persistence;

public sealed class PolicyProjectSummaryRepository(
    IPolicyModuleDbContext dbContext
) : IPolicyProjectSummaryRepository
{
    public async Task<List<PolicyProjectSummary>> List()
    {
        var summaries = await dbContext.PolicyProjectSummaries
            .AsNoTracking()
            .OrderBy(summary => summary.ProjectName)
            .ThenBy(summary => summary.ProjectAggregateId)
            .ToListAsync();

        return summaries
            .Select(
                summary => new PolicyProjectSummary(
                    summary.ProjectAggregateId,
                    summary.ProjectName,
                    JsonSerializer.Deserialize<List<string>>(
                        summary.RepositoryPathsJson
                    ) ?? []
                )
            )
            .ToList();
    }

    public async Task Replace(List<ProjectPoliciesStateData> projects)
    {
        var projectIds = projects
            .Select(project => project.Id.Value)
            .ToList();

        await dbContext.PolicyProjectSummaries
            .Where(
                summary => projectIds.Contains(
                    summary.ProjectAggregateId
                )
            )
            .ExecuteDeleteAsync();

        await dbContext.PolicyProjectSummaries.AddRangeAsync(
            projects
                .Where(project => !project.IsDeleted)
                .Select(
                    project => new PolicyProjectSummaryEntry
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
    }
}
