using FeatureModule.Domain;
using FeatureModule.Persistence.Interfaces;
using FeatureModule.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using SharedModule.Persistence;

namespace FeatureModule.Persistence;

public sealed class FeatureSummaryRepository(
    IFeatureModuleDbContext dbContext
) : IFeatureSummaryRepository
{
    public Task<List<FeatureSummary>> List(
        CancellationToken cancellationToken = default
    ) =>
        dbContext.FeatureSummaries
            .AsNoTracking()
            .OrderBy(feature => feature.Name)
            .ThenBy(feature => feature.FeatureAggregateId)
            .Select(ToReadModelExpression)
            .ToListAsync(cancellationToken);

    public Task<FeatureSummary?> GetByName(
        string name,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedName = name.Trim().ToUpperInvariant();

        return dbContext.FeatureSummaries
            .AsNoTracking()
            .Where(feature => feature.Name.ToUpper() == normalizedName)
            .Select(ToReadModelExpression)
            .SingleOrDefaultAsync(cancellationToken);
    }

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
            .Select(ToReadModelExpression)
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
        var activeFeatures = features
            .Where(feature => !feature.IsDeleted)
            .ToList();
        var affectedRelations = await GetAffectedRelations(featureIds);
        var relationsToReAdd = await BuildRelationsToReAdd(
            features,
            activeFeatures,
            affectedRelations
        );

        await DeleteProjection(featureIds, affectedRelations);
        await ReAddProjection(activeFeatures, relationsToReAdd);

        if (transaction is not null)
            await transaction.CommitAsync();
    }

    private Task<List<EntityRelation>> GetAffectedRelations(
        List<Guid> featureIds
    ) =>
        dbContext.EntityRelations
            .Where(
                relation => featureIds.Contains(relation.EntityId)
                    || featureIds.Contains(relation.RelatedEntityId)
            )
            .ToListAsync();

    private async Task<List<EntityRelation>> BuildRelationsToReAdd(
        List<FeatureStateData> features,
        List<FeatureStateData> activeFeatures,
        List<EntityRelation> existingRelations
    )
    {
        var projectedFeatureIds = features
            .Select(feature => feature.Id.Value)
            .ToHashSet();
        var activeFeatureSummaries = activeFeatures.ToDictionary(
            feature => feature.Id.Value,
            feature => feature.Summary
        );
        var deletedFeatureIds = projectedFeatureIds
            .Except(activeFeatureSummaries.Keys)
            .ToHashSet();
        var featureSummaries = await FeatureSummaries(activeFeatures);
        var existingRelationSummaries = existingRelations.ToDictionary(
            relation => (
                relation.EntityId,
                relation.RelatedEntityId
            ),
            relation => relation.RelatedEntitySummary
        );

        var relations = existingRelations
            .Where(
                relation => !deletedFeatureIds.Contains(relation.EntityId)
                    && !deletedFeatureIds.Contains(
                        relation.RelatedEntityId
                    )
            )
            .Where(
                relation => !IsOwnedByProjectedFeature(
                    relation,
                    projectedFeatureIds
                )
            )
            .Select(
                relation => CopyRelation(
                    relation,
                    activeFeatureSummaries
                )
            )
            .ToList();

        relations.AddRange(
            activeFeatures.SelectMany(
                feature => ToEntityRelations(
                    feature,
                    featureSummaries,
                    existingRelationSummaries
                )
            )
        );

        return relations;
    }

    private async Task<Dictionary<Guid, string>> FeatureSummaries(
        List<FeatureStateData> activeFeatures
    )
    {
        var parentFeatureIds = activeFeatures
            .Where(feature => feature.ParentFeatureId is not null)
            .Select(feature => feature.ParentFeatureId!.Value.Value)
            .Distinct()
            .ToList();
        var summaries = parentFeatureIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await dbContext.FeatureSummaries
                .AsNoTracking()
                .Where(
                    summary => parentFeatureIds.Contains(
                        summary.FeatureAggregateId
                    )
                )
                .ToDictionaryAsync(
                    summary => summary.FeatureAggregateId,
                    summary => summary.Summary
                );

        foreach (var feature in activeFeatures)
            summaries[feature.Id.Value] = feature.Summary;

        return summaries;
    }

    private async Task DeleteProjection(
        List<Guid> featureIds,
        List<EntityRelation> relations
    )
    {
        dbContext.EntityRelations.RemoveRange(relations);
        await dbContext.FeatureSummaries
            .Where(
                summary => featureIds.Contains(
                    summary.FeatureAggregateId
                )
            )
            .ExecuteDeleteAsync();
        await dbContext.SaveChangesAsync();
    }

    private async Task ReAddProjection(
        List<FeatureStateData> features,
        List<EntityRelation> relations
    )
    {
        dbContext.FeatureSummaries.AddRange(
            features.Select(ToSummaryEntry)
        );
        dbContext.EntityRelations.AddRange(relations);
        await dbContext.SaveChangesAsync();
    }

    private static FeatureSummaryEntry ToSummaryEntry(
        FeatureStateData feature
    ) =>
        new()
        {
            FeatureAggregateId = feature.Id.Value,
            ProjectId = feature.ProjectId.Value,
            Name = feature.Name,
            Summary = feature.Summary,
            Status = feature.Status,
            CurrentPlanId = feature.CurrentPlanId?.Value,
            PlanCount = feature.Plans.Count,
            RecordCount = feature.Records.Count
        };

    private static List<EntityRelation> ToEntityRelations(
        FeatureStateData feature,
        Dictionary<Guid, string> featureSummaries,
        Dictionary<(Guid EntityId, Guid RelatedEntityId), string>
            existingRelationSummaries
    )
    {
        var relations = new List<EntityRelation>();

        if (feature.ParentFeatureId is { } parentFeatureId)
        {
            relations.AddRange(
                CreateRelationPair(
                    feature.Id.Value,
                    feature.Summary,
                    parentFeatureId.Value,
                    Summary(featureSummaries, parentFeatureId.Value),
                    FeatureEntityRelationTypes.ParentFeature,
                    FeatureEntityRelationTypes.Subfeature
                )
            );
        }

        foreach (var skillId in feature.RelatedSkillIds)
        {
            relations.AddRange(
                CreateRelationPair(
                    feature.Id.Value,
                    feature.Summary,
                    skillId.Value,
                    existingRelationSummaries.GetValueOrDefault(
                        (feature.Id.Value, skillId.Value),
                        string.Empty
                    ),
                    FeatureEntityRelationTypes.Skill,
                    FeatureEntityRelationTypes.Feature
                )
            );
        }

        return relations;
    }

    private static List<EntityRelation> CreateRelationPair(
        Guid entityId,
        string entitySummary,
        Guid relatedEntityId,
        string relatedEntitySummary,
        string relationType,
        string reverseRelationType
    ) =>
        [
            new()
            {
                EntityId = entityId,
                RelatedEntityId = relatedEntityId,
                RelationType = relationType,
                RelatedEntitySummary = relatedEntitySummary
            },
            new()
            {
                EntityId = relatedEntityId,
                RelatedEntityId = entityId,
                RelationType = reverseRelationType,
                RelatedEntitySummary = entitySummary
            }
        ];

    private static bool IsOwnedByProjectedFeature(
        EntityRelation relation,
        HashSet<Guid> projectedFeatureIds
    ) =>
        relation.RelationType switch
        {
            FeatureEntityRelationTypes.ParentFeature
                or FeatureEntityRelationTypes.Skill =>
                projectedFeatureIds.Contains(relation.EntityId),
            FeatureEntityRelationTypes.Subfeature
                or FeatureEntityRelationTypes.Feature =>
                projectedFeatureIds.Contains(relation.RelatedEntityId),
            _ => false
        };

    private static EntityRelation CopyRelation(
        EntityRelation relation,
        Dictionary<Guid, string> activeFeatureSummaries
    ) =>
        new()
        {
            EntityId = relation.EntityId,
            RelatedEntityId = relation.RelatedEntityId,
            RelationType = relation.RelationType,
            RelatedEntitySummary = activeFeatureSummaries.GetValueOrDefault(
                relation.RelatedEntityId,
                relation.RelatedEntitySummary
            )
        };

    private static string Summary(
        Dictionary<Guid, string> summaries,
        Guid entityId
    ) => summaries.GetValueOrDefault(entityId, string.Empty);

    private static System.Linq.Expressions.Expression<
        Func<FeatureSummaryEntry, FeatureSummary>
    > ToReadModelExpression =>
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
}
