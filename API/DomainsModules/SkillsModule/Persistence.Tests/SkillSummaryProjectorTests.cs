using EventSourcing.Shared.Models;
using Microsoft.EntityFrameworkCore;
using SkillsModule.Domain;
using SkillsModule.Persistence.Models;

namespace SkillsModule.Persistence.Tests;

public sealed class SkillSummaryProjectorTests
{
    [Fact]
    public async Task Update_replaces_active_summaries_and_removes_deleted_skills()
    {
        await using var context = CreateContext();
        var repository = new SkillSummaryRepository(context);
        var projector = new SkillSummaryProjector(repository);
        var first = CreateSkill(
            "11111111-1111-1111-1111-111111111111",
            "beta"
        );
        var second = CreateSkill(
            "22222222-2222-2222-2222-222222222222",
            "alpha"
        );

        await projector.Update(
            [CreateStateInfo(first), CreateStateInfo(second)]
        );

        Assert.Equal(
            ["alpha", "beta"],
            (await repository.List())
                .Select(skill => skill.Name)
                .ToList()
        );

        first.Name = "gamma";
        await projector.Update([CreateStateInfo(first)]);

        Assert.Equal(
            ["alpha", "gamma"],
            (await repository.List())
                .Select(skill => skill.Name)
                .ToList()
        );

        first.IsDeleted = true;
        await projector.Update([CreateStateInfo(first)]);

        Assert.Equal(
            ["alpha"],
            (await repository.List())
                .Select(skill => skill.Name)
                .ToList()
        );
    }

    private static TestSkillsDbContext CreateContext()
    {
        var context = new TestSkillsDbContext(
            new DbContextOptionsBuilder<TestSkillsDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options
        );
        context.Database.OpenConnection();
        context.Database.EnsureCreated();

        return context;
    }

    private static SkillStateData CreateSkill(
        string id,
        string name
    ) =>
        new(
            AggregateId.FromDatabaseGuid(Guid.Parse(id))
        )
        {
            Name = name
        };

    private static StateInfo CreateStateInfo(
        SkillStateData skill
    ) =>
        StateInfo.Create(
            skill,
            "skills-state-machine",
            skill.Id
        );

    private sealed class TestSkillsDbContext(
        DbContextOptions<TestSkillsDbContext> options
    ) : DbContext(options), ISkillsModuleDbContext
    {
        public DbSet<SkillSummaryEntry> SkillSummaries =>
            Set<SkillSummaryEntry>();

        protected override void OnModelCreating(
            ModelBuilder modelBuilder
        )
        {
            modelBuilder
                .Entity<SkillSummaryEntry>()
                .HasKey(summary => summary.Id);
            modelBuilder
                .Entity<SkillSummaryEntry>()
                .HasIndex(summary => summary.SkillAggregateId)
                .IsUnique();
        }
    }
}
