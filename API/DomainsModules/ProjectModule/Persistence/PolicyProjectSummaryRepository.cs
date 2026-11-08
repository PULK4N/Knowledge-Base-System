using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ProjectModule.Domain;
using ProjectModule.Persistence.Interfaces;
using ProjectModule.Persistence.Models;
using SharedModule.Persistence;

namespace ProjectModule.Persistence;

public sealed class PolicyProjectSummaryRepository(IProjectModuleDbContext dbContext)
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
            .OrderByDescending(summary => summary.UpdatedAt)
            .ThenBy(summary => summary.ProjectName)
            .ThenBy(summary => summary.ProjectAggregateId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var items = entries
            .Select(ToReadModel)
            .ToList();

        return new PolicyProjectSummarySearchResult(items, totalCount);
    }

    public Task<List<Guid>> GetSkillIds(
        Guid projectId,
        CancellationToken cancellationToken = default
    ) =>
        dbContext.EntityRelations
            .AsNoTracking()
            .Where(
                relation => relation.EntityId == projectId
                    && relation.RelationType == ProjectEntityRelationTypes.Skill
            )
            .Select(relation => relation.RelatedEntityId)
            .ToListAsync(cancellationToken);

    public async Task Write(List<PolicyProjectSummaryUpdate> updates)
    {
        var context =
            dbContext as DbContext
            ?? throw new InvalidOperationException(
                $"{nameof(IProjectModuleDbContext)} must be implemented by a {nameof(DbContext)}."
            );
        await using var transaction = context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync()
            : null;
        var projectIds = updates.Select(update => update.State.Id.Value).ToList();
        var activeProjects = updates
            .Where(update => !update.State.IsDeleted)
            .ToList();

        await dbContext
            .PolicyProjectSummaries
            .Where(summary => projectIds.Contains(summary.ProjectAggregateId))
            .ExecuteDeleteAsync();
        var skillRelations = await DeleteSkillRelations(projectIds);

        dbContext.EntityRelations.AddRange(
            activeProjects.SelectMany(
                update => ToSkillRelations(update.State, skillRelations)
            )
        );
        await dbContext
            .PolicyProjectSummaries
            .AddRangeAsync(
                activeProjects
                    .Select(
                        update =>
                            new PolicyProjectSummaryEntry
                            {
                                ProjectAggregateId = update.State.Id.Value,
                                ProjectName = update.State.ProjectName,
                                RepositoryPathsJson = JsonSerializer.Serialize(
                                    update.State.RepositoryPaths
                                ),
                                UpdatedAt = update.UpdatedAt
                            }
                    )
            );
        await dbContext.SaveChangesAsync();

        if (transaction is not null)
            await transaction.CommitAsync();
    }

    /// <summary>
    /// The project owns both rows of each project-skill relation, so it deletes
    /// them all and re-adds the current ones. Returns the deleted rows so each
    /// skill keeps the summary it was written with.
    /// </summary>
    private async Task<List<EntityRelation>> DeleteSkillRelations(
        List<Guid> projectIds
    )
    {
        var relations = await dbContext.EntityRelations
            .Where(
                relation =>
                    (
                        relation.RelationType == ProjectEntityRelationTypes.Skill
                        && projectIds.Contains(relation.EntityId)
                    )
                    || (
                        relation.RelationType == ProjectEntityRelationTypes.Project
                        && projectIds.Contains(relation.RelatedEntityId)
                    )
            )
            .ToListAsync();

        dbContext.EntityRelations.RemoveRange(relations);
        await dbContext.SaveChangesAsync();

        return relations;
    }

    private static List<EntityRelation> ToSkillRelations(
        ProjectStateData project,
        List<EntityRelation> previousRelations
    ) =>
        project.RelatedSkillIds
            .SelectMany(
                skillId => new List<EntityRelation>
                {
                    new()
                    {
                        EntityId = project.Id.Value,
                        RelatedEntityId = skillId.Value,
                        RelationType = ProjectEntityRelationTypes.Skill,
                        RelatedEntitySummary = previousRelations
                            .FirstOrDefault(
                                relation => relation.EntityId == project.Id.Value
                                    && relation.RelatedEntityId == skillId.Value
                            )
                            ?.RelatedEntitySummary ?? string.Empty
                    },
                    new()
                    {
                        EntityId = skillId.Value,
                        RelatedEntityId = project.Id.Value,
                        RelationType = ProjectEntityRelationTypes.Project,
                        RelatedEntitySummary = project.ProjectName
                    }
                }
            )
            .ToList();

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
