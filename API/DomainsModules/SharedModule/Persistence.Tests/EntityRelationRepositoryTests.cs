using Microsoft.EntityFrameworkCore;
using Xunit;

namespace SharedModule.Persistence.Tests;

public sealed class EntityRelationRepositoryTests
{
    [Fact]
    public async Task Add_writes_both_directions_with_related_summaries()
    {
        await using var context = CreateContext();
        var repository = new EntityRelationRepository(context);
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        await repository.Add(
            new EntityRelationWrite(
                parentId,
                "Parent summary",
                childId,
                "Child summary",
                "Child",
                "Parent"
            )
        );

        var relations = await context.EntityRelations
            .AsNoTracking()
            .OrderBy(relation => relation.RelationType)
            .ToListAsync();
        Assert.Collection(
            relations,
            childRelation =>
            {
                Assert.Equal(parentId, childRelation.EntityId);
                Assert.Equal(childId, childRelation.RelatedEntityId);
                Assert.Equal("Child", childRelation.RelationType);
                Assert.Equal(
                    "Child summary",
                    childRelation.RelatedEntitySummary
                );
            },
            parentRelation =>
            {
                Assert.Equal(childId, parentRelation.EntityId);
                Assert.Equal(parentId, parentRelation.RelatedEntityId);
                Assert.Equal("Parent", parentRelation.RelationType);
                Assert.Equal(
                    "Parent summary",
                    parentRelation.RelatedEntitySummary
                );
            }
        );
    }

    [Fact]
    public async Task Add_replaces_an_existing_pair_without_duplicates()
    {
        await using var context = CreateContext();
        var repository = new EntityRelationRepository(context);
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var relation = new EntityRelationWrite(
            parentId,
            "Original parent summary",
            childId,
            "Original child summary",
            "Child",
            "Parent"
        );

        await repository.Add(relation);
        await repository.Add(
            relation with
            {
                EntitySummary = "Updated parent summary",
                RelatedEntitySummary = "Updated child summary",
                RelationType = "Descendant",
                ReverseRelationType = "Ancestor"
            }
        );

        var relations = await context.EntityRelations
            .AsNoTracking()
            .ToListAsync();
        Assert.Equal(2, relations.Count);
        var forwardRelation = relations.Single(
            relation => relation.EntityId == parentId
        );
        var reverseRelation = relations.Single(
            relation => relation.EntityId == childId
        );
        Assert.Equal("Descendant", forwardRelation.RelationType);
        Assert.Equal(
            "Updated child summary",
            forwardRelation.RelatedEntitySummary
        );
        Assert.Equal("Ancestor", reverseRelation.RelationType);
        Assert.Equal(
            "Updated parent summary",
            reverseRelation.RelatedEntitySummary
        );
    }

    private static TestDbContext CreateContext()
    {
        var context = new TestDbContext(
            new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options
        );
        context.Database.OpenConnection();
        context.Database.EnsureCreated();

        return context;
    }

    private sealed class TestDbContext(
        DbContextOptions<TestDbContext> options
    ) : DbContext(options), IEntityRelationDbContext
    {
        public DbSet<EntityRelation> EntityRelations =>
            Set<EntityRelation>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
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
