using System.Linq.Expressions;
using ActionModule.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ActionModule.Persistence.Tests;

public sealed class EntityQueryExecutorTests
{
    [Fact]
    public async Task Execute_applies_filters_search_sort_paging_and_projection()
    {
        await using var context = CreateContext();
        context.Entries.AddRange(
            new TestEntry { Id = 4, Group = 1, Name = "Beta" },
            new TestEntry { Id = 2, Group = 1, Name = "Alpha two" },
            new TestEntry { Id = 1, Group = 1, Name = "Alpha one" },
            new TestEntry { Id = 3, Group = 2, Name = "Alpha other" }
        );
        await context.SaveChangesAsync();
        var request = new EntityQuery<TestFilter, TestSort>(
            new PageRequest(2, 1),
            "  alpha  ",
            new TestFilter(1),
            new SortRequest<TestSort>(
                TestSort.Name,
                SortDirection.Ascending
            )
        );

        var result = await EntityQueryExecutor.Execute(
            context.Entries,
            request,
            new TestProfile()
        );

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(1, result.PageSize);
        Assert.Equal("Alpha two", Assert.Single(result.Items));
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
    ) : DbContext(options)
    {
        public DbSet<TestEntry> Entries => Set<TestEntry>();
    }

    private sealed class TestEntry
    {
        public int Id { get; set; }
        public int Group { get; set; }
        public required string Name { get; set; }
    }

    private sealed record TestFilter(int Group);

    private enum TestSort
    {
        Name
    }

    private sealed class TestProfile
        : IEntityQueryProfile<
            TestEntry,
            TestFilter,
            TestSort,
            string
        >
    {
        public IQueryable<TestEntry> ApplyFilters(
            IQueryable<TestEntry> query,
            TestFilter filters
        ) => query.Where(entry => entry.Group == filters.Group);

        public IQueryable<TestEntry> ApplySearch(
            IQueryable<TestEntry> query,
            string? search
        ) =>
            search is null
                ? query
                : query.Where(
                    entry => entry.Name.ToLower().Contains(search.ToLower())
                );

        public IOrderedQueryable<TestEntry> ApplySort(
            IQueryable<TestEntry> query,
            SortRequest<TestSort> sort
        ) =>
            sort.Direction == SortDirection.Ascending
                ? query
                    .OrderBy(entry => entry.Name)
                    .ThenBy(entry => entry.Id)
                : query
                    .OrderByDescending(entry => entry.Name)
                    .ThenByDescending(entry => entry.Id);

        public Expression<Func<TestEntry, string>> Projection =>
            entry => entry.Name;
    }
}
