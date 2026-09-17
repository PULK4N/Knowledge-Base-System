using EventSourcing.Shared.Models;
using MemoryModule.Domain;
using MemoryModule.Domain.Models;
using Microsoft.EntityFrameworkCore;
using SharedModule.Persistence;

namespace MemoryModule.Persistence.Tests;

public sealed class MemoryEntityRelationProjectorTests
{
    [Fact]
    public async Task Update_projects_every_entity_type_in_both_directions_and_replays_without_duplicates()
    {
        await using var context = CreateContext();
        var projector = CreateProjector(context);
        var memory = CreateMemory();
        memory.RelatedEntities = Enum.GetValues<MemoryEntityType>()
            .Select(type => new MemoryRelatedEntity(type, new MemoryEntityId(Guid.NewGuid())))
            .ToHashSet();
        var first = memory.RelatedEntities.First();
        memory.RelatedEntities.Add(first with { Type = MemoryEntityType.Policy });

        await projector.Update([ToStateInfo(memory)]);
        memory.ChatSummary.Summary = "Updated session summary";
        await context.EntityRelations
            .Where(row => row.EntityId == memory.Id.Value && row.RelatedEntityId == first.Id.Value)
            .ExecuteUpdateAsync(update => update.SetProperty(row => row.RelatedEntitySummary, "Known entity summary"));
        await projector.Update([ToStateInfo(memory)]);

        var rows = await context.EntityRelations.AsNoTracking().ToListAsync();
        Assert.Equal(Enum.GetValues<MemoryEntityType>().Length * 2, rows.Count);
        foreach (var entityId in memory.RelatedEntities.Select(entity => entity.Id).Distinct())
        {
            var forward = Assert.Single(rows, row =>
                row.EntityId == memory.Id.Value && row.RelatedEntityId == entityId.Value);
            Assert.Equal(MemoryEntityRelationRepository.ChangedEntity, forward.RelationType);
            var reverse = Assert.Single(rows, row =>
                row.EntityId == entityId.Value && row.RelatedEntityId == memory.Id.Value);
            Assert.Equal(MemoryEntityRelationRepository.ChangedInMemory, reverse.RelationType);
            Assert.Equal("Updated session summary", reverse.RelatedEntitySummary);
        }
        Assert.Equal("Known entity summary", rows.Single(row =>
            row.EntityId == memory.Id.Value && row.RelatedEntityId == first.Id.Value).RelatedEntitySummary);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Update_removes_owned_rows_for_empty_or_deleted_memory_and_preserves_other_relations(bool deleted)
    {
        await using var context = CreateContext();
        var projector = CreateProjector(context);
        var memory = CreateMemory();
        await projector.Update([ToStateInfo(memory)]);
        var unrelated = new EntityRelation
        {
            EntityId = memory.Id.Value,
            RelatedEntityId = Guid.NewGuid(),
            RelationType = "Other",
            RelatedEntitySummary = "Keep this relation"
        };
        context.EntityRelations.Add(unrelated);
        await context.SaveChangesAsync();

        memory.IsDeleted = deleted;
        if (!deleted)
            memory.RelatedEntities.Clear();
        await projector.Update([ToStateInfo(memory)]);

        var remaining = Assert.Single(await context.EntityRelations.AsNoTracking().ToListAsync());
        Assert.Equal(unrelated.Id, remaining.Id);
        Assert.Equal("Keep this relation", remaining.RelatedEntitySummary);
    }

    [Fact]
    public async Task Update_handles_two_memories_with_the_same_related_entity()
    {
        await using var context = CreateContext();
        var projector = CreateProjector(context);
        var first = CreateMemory();
        var second = CreateMemory();
        second.RelatedEntities = first.RelatedEntities.ToHashSet();
        var states = new List<StateInfo> { ToStateInfo(first), ToStateInfo(second) };

        await projector.Update(states);
        await projector.Update(states);

        Assert.Equal(4, await context.EntityRelations.CountAsync());
    }

    [Fact]
    public async Task Update_rolls_back_deletions_when_replacement_fails()
    {
        await using var context = CreateContext();
        var projector = CreateProjector(context);
        var memory = CreateMemory();
        await projector.Update([ToStateInfo(memory)]);
        var originalId = Assert.Single(memory.RelatedEntities).Id;
        memory.RelatedEntities = [new(MemoryEntityType.FeatureRecord, new MemoryEntityId(Guid.NewGuid()))];
        context.FailSave = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => projector.Update([ToStateInfo(memory)]));

        var rows = await context.EntityRelations.AsNoTracking().ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.Contains(rows, row => row.RelatedEntityId == originalId.Value);
        Assert.Contains(rows, row => row.EntityId == originalId.Value);
    }

    private static MemoryStateData CreateMemory() => new(AggregateId.FromDatabaseGuid(Guid.NewGuid()))
    {
        ChatSummary = new ChatSummary { Summary = "Session summary" },
        RelatedEntities = [new(MemoryEntityType.FeatureResearchDiscovery, new MemoryEntityId(Guid.NewGuid()))]
    };

    private static StateInfo ToStateInfo(MemoryStateData memory) =>
        StateInfo.Create(memory, "memory-state-machine", memory.Id);

    private static MemoryEntityRelationProjector CreateProjector(TestDbContext context) =>
        new(new MemoryEntityRelationRepository(context));

    private static TestDbContext CreateContext()
    {
        var context = new TestDbContext(new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite("Data Source=:memory:").Options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options)
        : DbContext(options), IEntityRelationDbContext
    {
        public DbSet<EntityRelation> EntityRelations => Set<EntityRelation>();
        public bool FailSave { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            FailSave
                ? throw new InvalidOperationException("Simulated replacement failure")
                : base.SaveChangesAsync(cancellationToken);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EntityRelation>().HasKey(row => row.Id);
            modelBuilder.Entity<EntityRelation>()
                .HasIndex(row => new { row.EntityId, row.RelatedEntityId }).IsUnique();
            modelBuilder.Entity<EntityRelation>().Property(row => row.RelationType).HasMaxLength(20);
        }
    }
}
