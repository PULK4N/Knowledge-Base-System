using EventSourcing.Shared.Models;
using FeatureModule.Domain;
using FeatureModule.Domain.Models;
using FeatureModule.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using SharedModule.Persistence;

namespace FeatureModule.Persistence.Tests;

public sealed class FeatureSummaryProjectorTests
{
    [Fact]
    public async Task Update_projects_searchable_feature_summaries_and_removes_deleted_features()
    {
        await using var context = CreateContext();
        var repository = new FeatureSummaryRepository(context);
        var projector = new FeatureSummaryProjector(repository);
        var first = CreateFeature(
            "11111111-1111-1111-1111-111111111111",
            "Feature journal",
            "Trace decisions"
        );
        var second = CreateFeature(
            "22222222-2222-2222-2222-222222222222",
            "Search page",
            "Find feature work"
        );

        await projector.Update(
            [CreateStateInfo(first), CreateStateInfo(second)]
        );

        var result = await repository.Search(1, 10, "TRACE");
        var summary = Assert.Single(result.Items);
        Assert.Equal(first.Id.Value, summary.FeatureId);
        Assert.Equal(1, summary.PlanCount);
        Assert.Equal(1, summary.RecordCount);
        Assert.Equal(
            ["Feature journal", "Search page"],
            (await repository.List())
                .Select(feature => feature.Name)
                .ToList()
        );
        Assert.Equal(
            second.Id.Value,
            (await repository.GetByName("  SEARCH PAGE "))?.FeatureId
        );
        Assert.Null(await repository.GetByName("missing"));

        first.IsDeleted = true;
        await projector.Update([CreateStateInfo(first)]);

        Assert.Equal(
            [second.Id.Value],
            (await repository.Search(1, 10, null))
                .Items
                .Select(feature => feature.FeatureId)
                .ToList()
        );
    }

    [Fact]
    public async Task Update_replaces_bidirectional_feature_and_skill_relations()
    {
        await using var context = CreateContext();
        var skillId = Guid.Parse(
            "33333333-3333-3333-3333-333333333333"
        );
        var repository = new FeatureSummaryRepository(context);
        var projector = new FeatureSummaryProjector(repository);
        var parent = CreateFeature(
            "11111111-1111-1111-1111-111111111111",
            "Parent feature",
            "Parent summary"
        );
        var child = CreateFeature(
            "22222222-2222-2222-2222-222222222222",
            "Child feature",
            "Child summary"
        );
        child.ParentFeatureId = parent.Id;
        child.RelatedSkillIds = [
            AggregateId.FromDatabaseGuid(skillId)
        ];

        await projector.Update(
            [CreateStateInfo(parent), CreateStateInfo(child)]
        );

        var relations = await context.EntityRelations
            .AsNoTracking()
            .ToListAsync();
        Assert.Equal(4, relations.Count);
        AssertRelation(
            relations,
            child.Id.Value,
            parent.Id.Value,
            FeatureEntityRelationTypes.ParentFeature,
            "Parent summary"
        );
        AssertRelation(
            relations,
            parent.Id.Value,
            child.Id.Value,
            FeatureEntityRelationTypes.Subfeature,
            "Child summary"
        );
        AssertRelation(
            relations,
            child.Id.Value,
            skillId,
            FeatureEntityRelationTypes.Skill,
            string.Empty
        );
        AssertRelation(
            relations,
            skillId,
            child.Id.Value,
            FeatureEntityRelationTypes.Feature,
            "Child summary"
        );

        parent.Summary = "Updated parent summary";
        await projector.Update([CreateStateInfo(parent)]);

        relations = await context.EntityRelations
            .AsNoTracking()
            .ToListAsync();
        Assert.Equal(4, relations.Count);
        AssertRelation(
            relations,
            child.Id.Value,
            parent.Id.Value,
            FeatureEntityRelationTypes.ParentFeature,
            "Updated parent summary"
        );

        child.ParentFeatureId = null;
        child.RelatedSkillIds.Clear();
        await projector.Update([CreateStateInfo(child)]);

        Assert.Empty(
            await context.EntityRelations.AsNoTracking().ToListAsync()
        );
    }

    private static void AssertRelation(
        List<EntityRelation> relations,
        Guid entityId,
        Guid relatedEntityId,
        string relationType,
        string relatedEntitySummary
    )
    {
        var relation = Assert.Single(
            relations,
            relation => relation.EntityId == entityId
                && relation.RelatedEntityId == relatedEntityId
        );
        Assert.Equal(relationType, relation.RelationType);
        Assert.Equal(
            relatedEntitySummary,
            relation.RelatedEntitySummary
        );
    }

    private static TestFeatureDbContext CreateContext()
    {
        var context = new TestFeatureDbContext(
            new DbContextOptionsBuilder<TestFeatureDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options
        );
        context.Database.OpenConnection();
        context.Database.EnsureCreated();

        return context;
    }

    private static FeatureStateData CreateFeature(
        string id,
        string name,
        string summary
    )
    {
        var feature = new FeatureStateData(
            AggregateId.FromDatabaseGuid(Guid.Parse(id))
        )
        {
            ProjectId = AggregateId.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            ),
            Name = name,
            Summary = summary,
            Status = "In progress"
        };
        feature.Plans.Add(
            new FeaturePlan
            {
                Id = FeaturePlanId.FromDatabaseGuid(
                    Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
                ),
                Title = "Plan"
            }
        );
        feature.Records.Add(
            new FeatureRecord
            {
                Id = FeatureRecordId.FromDatabaseGuid(
                    Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")
                ),
                UserMessage = "Why?",
                AiAnswer = "Because."
            }
        );

        return feature;
    }

    private static StateInfo CreateStateInfo(
        FeatureStateData feature
    ) =>
        StateInfo.Create(
            feature,
            "features-state-machine",
            feature.Id
        );

    private sealed class TestFeatureDbContext(
        DbContextOptions<TestFeatureDbContext> options
    ) : DbContext(options), IFeatureModuleDbContext
    {
        public DbSet<FeatureSummaryEntry> FeatureSummaries =>
            Set<FeatureSummaryEntry>();
        public DbSet<FeatureSearchEntry> FeatureSearchEntries =>
            Set<FeatureSearchEntry>();
        public DbSet<EntityRelation> EntityRelations =>
            Set<EntityRelation>();

        protected override void OnModelCreating(
            ModelBuilder modelBuilder
        )
        {
            modelBuilder
                .Entity<FeatureSummaryEntry>()
                .HasKey(summary => summary.Id);
            modelBuilder
                .Entity<FeatureSummaryEntry>()
                .HasIndex(summary => summary.FeatureAggregateId)
                .IsUnique();
            modelBuilder
                .Entity<FeatureSummaryEntry>()
                .HasIndex(summary => summary.Name)
                .IsUnique();
            modelBuilder
                .Entity<FeatureSearchEntry>()
                .HasKey(entry => entry.Id);
            modelBuilder.Entity<EntityRelation>(
                relation =>
                {
                    relation.HasKey(entry => entry.Id);
                    relation.HasIndex(
                        entry => new
                        {
                            entry.EntityId,
                            entry.RelatedEntityId
                        }
                    ).IsUnique();
                }
            );
        }
    }
}
