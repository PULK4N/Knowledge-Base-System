using ActionModule.Shared.Models;
using EventSourcing.Shared.Models;
using SkillsModule.Application.DTOs;
using SkillsModule.Application.Queries;
using SkillsModule.Persistence.Interfaces;

namespace SkillsModule.Application.Tests;

public sealed class SearchSkillContentQueryTests
{
    private static readonly Executor Executor =
        new()
        {
            Id = EventExecutor.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            )
        };

    [Fact]
    public async Task Execute_returns_hybrid_search_matches()
    {
        var skillId = AggregateId.FromDatabaseGuid(
            Guid.Parse("11111111-1111-1111-1111-111111111111")
        );
        var search = new FakeSkillSearch(
            [
                new SkillSearchResult(
                    new SkillSearchCandidate(
                        skillId,
                        "event-sourcing",
                        "references/persistence.md",
                        2,
                        "Use PostgreSQL projections."
                    ),
                    0.031,
                    1,
                    3
                )
            ]
        );
        var query = new SearchSkillContentQuery(search)
        {
            SearchText = "PostgreSQL event projections",
            ResultCount = 4
        };

        var results = await query.Execute(Executor);

        Assert.Equal("PostgreSQL event projections", search.LastQuery);
        Assert.Equal(4, search.LastOptions!.ResultCount);
        Assert.Equal(50, search.LastOptions.CandidateCount);
        Assert.Equal(
            new SkillSearchMatchDto(
                skillId.Value,
                "event-sourcing",
                "references/persistence.md",
                2,
                "Use PostgreSQL projections.",
                0.031,
                1,
                3
            ),
            Assert.Single(results)
        );
    }

    [Theory]
    [InlineData("", SearchSkillContentQuery.DefaultResultCount)]
    [InlineData("query", SearchSkillContentQuery.MinimumResultCount - 1)]
    [InlineData("query", SearchSkillContentQuery.MaximumResultCount + 1)]
    public async Task CanExecute_rejects_invalid_searches(
        string searchText,
        int resultCount
    )
    {
        var query = new SearchSkillContentQuery(new FakeSkillSearch([]))
        {
            SearchText = searchText,
            ResultCount = resultCount
        };

        Assert.False(await query.CanExecute(Executor));
    }

    private sealed class FakeSkillSearch(
        IReadOnlyList<SkillSearchResult> results
    ) : ISkillSearch
    {
        public string? LastQuery { get; private set; }
        public HybridSkillSearchOptions? LastOptions { get; private set; }

        public Task<IReadOnlyList<SkillSearchResult>> Search(
            string query,
            HybridSkillSearchOptions? options = null,
            CancellationToken cancellationToken = default
        )
        {
            LastQuery = query;
            LastOptions = options;

            return Task.FromResult(results);
        }
    }
}
