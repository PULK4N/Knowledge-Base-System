using ActionModule.Shared.Models;
using EventSourcing.Shared.Models;
using FeatureModule.Contracts;
using FeatureModule.Domain;
using FeatureModule.Domain.Models;
using FeatureModule.Persistence.Interfaces;
using FeatureModule.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using SharedModule.Persistence;

namespace FeatureModule.Persistence.Tests;

public sealed class FeatureSearchProjectorTests
{
    [Fact]
    public async Task Update_writes_a_separate_filterable_search_projection()
    {
        await using var context = CreateContext();
        var repository = new FeatureSearchRepository(context);
        var projector = new FeatureSearchProjector(repository);
        var projectId = Guid.Parse(
            "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
        );
        var first = CreateFeature(
            "11111111-1111-1111-1111-111111111111",
            projectId,
            "Feature journal",
            "Trace decisions",
            "Planning",
            1,
            1
        );
        var second = CreateFeature(
            "22222222-2222-2222-2222-222222222222",
            projectId,
            "Query page",
            "Trace implementation",
            "Planning",
            2,
            2
        );
        var other = CreateFeature(
            "33333333-3333-3333-3333-333333333333",
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            "Other project",
            "Trace unrelated work",
            "Planning",
            3,
            3
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
                new FeatureSearchFilters(projectId),
                " trace ",
                FeatureSearchSortField.RecordCount,
                SortDirection.Descending,
                pageSize: 1
            )
        );

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(second.Id.Value, Assert.Single(result.Items).FeatureId);
        Assert.Empty(context.FeatureSummaries);

        second.IsDeleted = true;
        await projector.Update([CreateStateInfo(second, 3)]);

        var afterDeletion = await repository.Search(
            CreateRequest(
                new FeatureSearchFilters(projectId),
                "trace",
                FeatureSearchSortField.Name,
                SortDirection.Ascending
            )
        );
        Assert.Equal(first.Id.Value, Assert.Single(afterDeletion.Items).FeatureId);

        var staleSecond = CreateFeature(
            "22222222-2222-2222-2222-222222222222",
            projectId,
            "Stale query page",
            "Trace stale implementation",
            "Planning",
            1,
            1
        );
        await projector.Update([CreateStateInfo(staleSecond, 2)]);

        var tombstone = await context.FeatureSearchEntries.SingleAsync(
            entry => entry.FeatureAggregateId == second.Id.Value
        );
        Assert.True(tombstone.IsDeleted);
        Assert.Equal(3, tombstone.ProjectedOrderNumber);
        var afterStaleUpdate = await repository.Search(
            CreateRequest(
                new FeatureSearchFilters(projectId),
                "trace",
                FeatureSearchSortField.Name,
                SortDirection.Ascending
            )
        );
        Assert.Equal(
            first.Id.Value,
            Assert.Single(afterStaleUpdate.Items).FeatureId
        );
    }

    [Theory]
    [InlineData(FeatureSearchSortField.Name, SortDirection.Ascending, 2)]
    [InlineData(FeatureSearchSortField.Name, SortDirection.Descending, 1)]
    [InlineData(FeatureSearchSortField.PlanCount, SortDirection.Ascending, 2)]
    [InlineData(FeatureSearchSortField.PlanCount, SortDirection.Descending, 3)]
    [InlineData(FeatureSearchSortField.RecordCount, SortDirection.Ascending, 1)]
    [InlineData(FeatureSearchSortField.RecordCount, SortDirection.Descending, 2)]
    public async Task Search_applies_each_allowlisted_sort(
        FeatureSearchSortField sortBy,
        SortDirection direction,
        int expectedFirstId
    )
    {
        await using var context = CreateContext();
        context.FeatureSearchEntries.AddRange(
            CreateEntry(1, "Zebra", "Planning", 2, 1),
            CreateEntry(2, "Alpha", "Active", 1, 3),
            CreateEntry(3, "Beta", "Review", 3, 2)
        );
        await context.SaveChangesAsync();

        var result = await new FeatureSearchRepository(context).Search(
            CreateRequest(
                new FeatureSearchFilters(null),
                null,
                sortBy,
                direction
            )
        );

        Assert.Equal(
            FeatureId(expectedFirstId),
            result.Items[0].FeatureId
        );
    }

    [Fact]
    public async Task Search_uses_feature_id_as_the_final_tie_breaker()
    {
        await using var context = CreateContext();
        context.FeatureSearchEntries.AddRange(
            CreateEntry(3, "Third", "Planning", 2, 1),
            CreateEntry(1, "First", "Planning", 2, 1)
        );
        await context.SaveChangesAsync();

        var result = await new FeatureSearchRepository(context).Search(
            CreateRequest(
                new FeatureSearchFilters(null),
                null,
                FeatureSearchSortField.PlanCount,
                SortDirection.Ascending
            )
        );

        Assert.Equal(
            [FeatureId(1), FeatureId(3)],
            result.Items.Select(feature => feature.FeatureId).ToList()
        );
    }

    [Fact]
    public async Task Update_does_not_replace_a_newer_projection_state()
    {
        await using var context = CreateContext();
        var projector = new FeatureSearchProjector(
            new FeatureSearchRepository(context)
        );
        var featureId = "11111111-1111-1111-1111-111111111111";
        var projectId = Guid.Parse(
            "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
        );
        var newer = CreateFeature(
            featureId,
            projectId,
            "Newer name",
            "Newer summary",
            "Newer status",
            1,
            1
        );
        var older = CreateFeature(
            featureId,
            projectId,
            "Older name",
            "Older summary",
            "Older status",
            0,
            0
        );

        await projector.Update([CreateStateInfo(newer, 2)]);
        await projector.Update([CreateStateInfo(older, 1)]);

        var entry = await context.FeatureSearchEntries.SingleAsync();
        Assert.Equal("Newer name", entry.Name);
        Assert.Equal(2, entry.ProjectedOrderNumber);
    }

    private static EntityQuery<FeatureSearchFilters, FeatureSearchSortField>
        CreateRequest(
            FeatureSearchFilters filters,
            string? search,
            FeatureSearchSortField sortBy,
            SortDirection direction,
            int pageSize = 25
        ) =>
            new(
                new PageRequest(1, pageSize),
                search,
                filters,
                new SortRequest<FeatureSearchSortField>(sortBy, direction)
            );

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
        Guid projectId,
        string name,
        string summary,
        string status,
        int planCount,
        int recordCount
    )
    {
        var feature = new FeatureStateData(
            AggregateId.FromDatabaseGuid(Guid.Parse(id))
        )
        {
            ProjectId = AggregateId.FromDatabaseGuid(projectId),
            Name = name,
            Summary = summary,
            Status = status
        };
        feature.Plans.AddRange(
            Enumerable.Range(1, planCount)
                .Select(
                    index => new FeaturePlan
                    {
                        Id = FeaturePlanId.FromDatabaseGuid(
                            Guid.Parse($"bbbbbbbb-bbbb-bbbb-bbbb-{index:D12}")
                        ),
                        Title = $"Plan {index}"
                    }
                )
        );
        feature.Records.AddRange(
            Enumerable.Range(1, recordCount)
                .Select(
                    index => new FeatureRecord
                    {
                        Id = FeatureRecordId.FromDatabaseGuid(
                            Guid.Parse($"cccccccc-cccc-cccc-cccc-{index:D12}")
                        ),
                        UserMessage = $"Question {index}",
                        AiAnswer = $"Answer {index}"
                    }
                )
        );

        return feature;
    }

    private static StateInfo CreateStateInfo(
        FeatureStateData feature,
        uint orderNumber = 0
    )
    {
        var stateInfo = StateInfo.Create(
            feature,
            "features-state-machine",
            feature.Id
        );
        stateInfo.CurrentOrderNumber = orderNumber;
        return stateInfo;
    }

    private static FeatureSearchEntry CreateEntry(
        int id,
        string name,
        string status,
        int planCount,
        int recordCount
    ) =>
        new()
        {
            FeatureAggregateId = FeatureId(id),
            ProjectId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            Summary = $"{name} summary",
            SearchText = $"{name} {name} summary".ToUpperInvariant(),
            Status = status,
            PlanCount = planCount,
            RecordCount = recordCount,
            ProjectedOrderNumber = 1
        };

    private static Guid FeatureId(int id) =>
        Guid.Parse($"{id:D8}-1111-1111-1111-111111111111");

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
    }
}
