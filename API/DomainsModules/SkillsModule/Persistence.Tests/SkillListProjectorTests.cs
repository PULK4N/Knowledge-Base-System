using ActionModule.Shared.Models;
using EventSourcing.Shared.Models;
using Microsoft.EntityFrameworkCore;
using SkillsModule.Contracts;
using SkillsModule.Domain;
using SkillsModule.Domain.Models;
using SkillsModule.Persistence.Models;
using SharedModule.Persistence;

namespace SkillsModule.Persistence.Tests;

public sealed class SkillListProjectorTests
{
    [Fact]
    public async Task Update_writes_a_separate_filterable_list_projection()
    {
        await using var context = CreateContext();
        var repository = new SkillListRepository(context);
        var projector = new SkillListProjector(repository);
        var first = CreateSkill(
            1,
            "Event journal",
            "Trace decisions",
            ["dotnet", "architecture"],
            referenceCount: 1,
            attachmentCount: 0
        );
        var second = CreateSkill(
            2,
            "Query page",
            "Trace implementation",
            ["DOTNET", "database"],
            referenceCount: 2,
            attachmentCount: 1
        );
        var other = CreateSkill(
            3,
            "Angular list",
            "Trace frontend state",
            ["angular"],
            referenceCount: 1,
            attachmentCount: 1
        );

        await projector.Update(
            [
                CreateStateInfo(first, 1),
                CreateStateInfo(second, 1),
                CreateStateInfo(other, 1)
            ]
        );

        var result = await repository.Search(
            CreateRequest(
                new SkillSearchFilters(" dotnet ", true, true),
                " trace ",
                SkillSearchSortField.AttachmentCount,
                SortDirection.Descending,
                pageSize: 1
            )
        );

        Assert.Equal(1, result.TotalCount);
        var item = Assert.Single(result.Items);
        Assert.Equal(second.Id.Value, item.SkillId);
        Assert.Equal(["database", "DOTNET"], item.Tags);
        Assert.Equal(2, item.ReferenceCount);
        Assert.Equal(1, item.AttachmentCount);
        Assert.Empty(context.SkillSummaries);

        second.IsDeleted = true;
        await projector.Update([CreateStateInfo(second, 3)]);

        var afterDeletion = await repository.Search(
            CreateRequest(
                new SkillSearchFilters("dotnet", null, null),
                "trace",
                SkillSearchSortField.Name,
                SortDirection.Ascending
            )
        );
        Assert.Equal(first.Id.Value, Assert.Single(afterDeletion.Items).SkillId);

        var staleSecond = CreateSkill(
            2,
            "Stale query page",
            "Trace stale implementation",
            ["dotnet"],
            referenceCount: 1,
            attachmentCount: 0
        );
        await projector.Update([CreateStateInfo(staleSecond, 2)]);

        var tombstone = await context.SkillListEntries.SingleAsync(
            entry => entry.SkillAggregateId == second.Id.Value
        );
        Assert.True(tombstone.IsDeleted);
        Assert.Equal(3, tombstone.ProjectedOrderNumber);
        Assert.Empty(
            await context.SkillListTags
                .Where(tag => tag.SkillListEntryId == tombstone.Id)
                .ToListAsync()
        );
    }

    [Theory]
    [InlineData(SkillSearchSortField.Name, SortDirection.Ascending, 1)]
    [InlineData(SkillSearchSortField.Name, SortDirection.Descending, 3)]
    [InlineData(SkillSearchSortField.ReferenceCount, SortDirection.Ascending, 1)]
    [InlineData(SkillSearchSortField.ReferenceCount, SortDirection.Descending, 2)]
    [InlineData(SkillSearchSortField.AttachmentCount, SortDirection.Ascending, 3)]
    [InlineData(SkillSearchSortField.AttachmentCount, SortDirection.Descending, 1)]
    public async Task Search_applies_each_allowlisted_sort(
        SkillSearchSortField sortBy,
        SortDirection direction,
        int expectedFirstId
    )
    {
        await using var context = CreateContext();
        context.SkillListEntries.AddRange(
            CreateEntry(3, "Zebra", 2, 1),
            CreateEntry(1, "Alpha", 1, 3),
            CreateEntry(2, "Beta", 3, 2)
        );
        await context.SaveChangesAsync();

        var result = await new SkillListRepository(context).Search(
            CreateRequest(
                new SkillSearchFilters(null, null, null),
                null,
                sortBy,
                direction
            )
        );

        Assert.Equal(SkillId(expectedFirstId), result.Items[0].SkillId);
    }

    [Fact]
    public async Task Search_uses_skill_id_as_the_final_tie_breaker()
    {
        await using var context = CreateContext();
        context.SkillListEntries.AddRange(
            CreateEntry(3, "Third", 2, 1),
            CreateEntry(1, "First", 2, 1)
        );
        await context.SaveChangesAsync();

        var result = await new SkillListRepository(context).Search(
            CreateRequest(
                new SkillSearchFilters(null, null, null),
                null,
                SkillSearchSortField.ReferenceCount,
                SortDirection.Ascending
            )
        );

        Assert.Equal(
            [SkillId(1), SkillId(3)],
            result.Items.Select(skill => skill.SkillId).ToList()
        );
    }

    [Fact]
    public async Task Update_does_not_replace_a_newer_projection_state()
    {
        await using var context = CreateContext();
        var projector = new SkillListProjector(
            new SkillListRepository(context)
        );
        var newer = CreateSkill(
            1,
            "Newer name",
            "Newer description",
            ["newer"],
            referenceCount: 1,
            attachmentCount: 1
        );
        var older = CreateSkill(
            1,
            "Older name",
            "Older description",
            ["older"],
            referenceCount: 0,
            attachmentCount: 0
        );

        await projector.Update([CreateStateInfo(newer, 2)]);
        await projector.Update([CreateStateInfo(older, 1)]);

        var entry = await context.SkillListEntries
            .Include(skill => skill.Tags)
            .SingleAsync();
        Assert.Equal("Newer name", entry.Name);
        Assert.Equal(2, entry.ProjectedOrderNumber);
        Assert.Equal("newer", Assert.Single(entry.Tags).Tag);
    }

    private static EntityQuery<SkillSearchFilters, SkillSearchSortField>
        CreateRequest(
            SkillSearchFilters filters,
            string? search,
            SkillSearchSortField sortBy,
            SortDirection direction,
            int pageSize = 25
        ) =>
            new(
                new PageRequest(1, pageSize),
                search,
                filters,
                new SortRequest<SkillSearchSortField>(sortBy, direction)
            );

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
        int id,
        string name,
        string description,
        List<string> tags,
        int referenceCount,
        int attachmentCount
    )
    {
        var skill = new SkillStateData(
            AggregateId.FromDatabaseGuid(SkillId(id))
        )
        {
            Name = name,
            Description = description,
            Tags = tags
        };
        foreach (var index in Enumerable.Range(1, referenceCount))
        {
            skill.References.Add(
                $"references/{index}.md",
                new SkillReference2($"Reference {index}")
            );
        }
        foreach (var index in Enumerable.Range(1, attachmentCount))
        {
            var fileId = FileId.FromDatabaseGuid(
                Guid.Parse($"aaaaaaaa-aaaa-aaaa-aaaa-{id:D6}{index:D6}")
            );
            skill.Attachments.Add(
                fileId,
                new Attachment
                {
                    Id = fileId,
                    Name = $"attachment-{index}.txt",
                    Size = index,
                    FileType = "text/plain",
                    Extension = ".txt"
                }
            );
        }

        return skill;
    }

    private static StateInfo CreateStateInfo(
        SkillStateData skill,
        uint orderNumber
    )
    {
        var stateInfo = StateInfo.Create(
            skill,
            "skills-state-machine",
            skill.Id
        );
        stateInfo.CurrentOrderNumber = orderNumber;
        return stateInfo;
    }

    private static SkillListEntry CreateEntry(
        int id,
        string name,
        int referenceCount,
        int attachmentCount
    ) =>
        new()
        {
            SkillAggregateId = SkillId(id),
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            Description = $"{name} description",
            SearchText = $"{name} {name} description".ToUpperInvariant(),
            ReferenceCount = referenceCount,
            AttachmentCount = attachmentCount,
            ProjectedOrderNumber = 1
        };

    private static Guid SkillId(int id) =>
        Guid.Parse($"{id:D8}-1111-1111-1111-111111111111");

    private sealed class TestSkillsDbContext(
        DbContextOptions<TestSkillsDbContext> options
    ) : DbContext(options), ISkillsModuleDbContext
    {
        public DbSet<SkillSummaryEntry> SkillSummaries =>
            Set<SkillSummaryEntry>();
        public DbSet<SkillListEntry> SkillListEntries =>
            Set<SkillListEntry>();
        public DbSet<SkillListTagEntry> SkillListTags =>
            Set<SkillListTagEntry>();
        public DbSet<EntityRelation> EntityRelations =>
            Set<EntityRelation>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SkillSummaryEntry>()
                .HasKey(summary => summary.Id);
            modelBuilder.Entity<SkillListEntry>()
                .HasKey(skill => skill.Id);
            modelBuilder.Entity<SkillListEntry>()
                .HasIndex(skill => skill.SkillAggregateId)
                .IsUnique();
            modelBuilder.Entity<SkillListEntry>()
                .HasMany(skill => skill.Tags)
                .WithOne()
                .HasForeignKey(tag => tag.SkillListEntryId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<SkillListTagEntry>()
                .HasKey(tag => tag.Id);
            modelBuilder.Entity<SkillListTagEntry>()
                .HasIndex(
                    tag => new
                    {
                        tag.SkillListEntryId,
                        tag.NormalizedTag
                    }
                )
                .IsUnique();
        }
    }
}
