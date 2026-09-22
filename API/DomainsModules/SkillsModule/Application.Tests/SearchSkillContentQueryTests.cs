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
        var otherSkillId = AggregateId.FromDatabaseGuid(
            Guid.Parse("22222222-2222-2222-2222-222222222222")
        );
        var topMatch = new SkillSearchResult(
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
        );
        var otherSkillMatch = new SkillSearchResult(
            topMatch.Skill with
            {
                SkillAggregateId = otherSkillId,
                SkillName = "angular-code-writter",
                SourcePath = "SKILL.md",
                ChunkIndex = 0,
                Text = "Use standalone components."
            },
            0.015,
            2,
            null
        );
        var search = new FakeSkillSearch(
            [topMatch],
            [topMatch, otherSkillMatch]
        );
        var query = new SearchSkillContentQuery(search)
        {
            SearchText = "PostgreSQL event projections",
            Keywords = ["PostgreSQL", "projections"]
        };

        var results = await query.Execute(Executor);

        Assert.Equal("PostgreSQL event projections", search.LastQuery);
        Assert.Equal(["PostgreSQL", "projections"], search.LastKeywords);
        Assert.Equal(5, search.LastOptions!.ResultCount);
        Assert.Equal(50, search.LastOptions.CandidateCount);
        Assert.Equal(5, search.LastOptions.SourceResultCount);
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
            Assert.Single(results.TopMatches)
        );
        Assert.Equal(
            [skillId.Value, otherSkillId.Value],
            results.DistinctSources.Select(match => match.SkillId).ToList()
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
            Keywords = ["skill"],
            ResultCount = resultCount
        };

        Assert.False(await query.CanExecute(Executor));
    }

    [Fact]
    public async Task CanExecute_rejects_a_search_without_keywords()
    {
        var query = new SearchSkillContentQuery(new FakeSkillSearch([]))
        {
            SearchText = "PostgreSQL event projections",
            Keywords = []
        };

        Assert.False(await query.CanExecute(Executor));
    }

    private sealed class FakeSkillSearch(
        IReadOnlyList<SkillSearchResult> results,
        IReadOnlyList<SkillSearchResult>? sourceResults = null
    ) : ISkillSearch
    {
        public string? LastQuery { get; private set; }
        public List<string>? LastKeywords { get; private set; }
        public HybridSkillSearchOptions? LastOptions { get; private set; }

        public Task<IReadOnlyList<SkillSearchResult>> Search(
            string query,
            List<string> keywords,
            HybridSkillSearchOptions? options = null,
            CancellationToken cancellationToken = default
        )
        {
            LastQuery = query;
            LastKeywords = keywords;
            LastOptions = options;

            return Task.FromResult(results);
        }

        public Task<SkillSearchResults> SearchWithSources(
            string query,
            List<string> keywords,
            HybridSkillSearchOptions? options = null,
            CancellationToken cancellationToken = default
        )
        {
            LastQuery = query;
            LastKeywords = keywords;
            LastOptions = options;

            return Task.FromResult(
                new SkillSearchResults(results, sourceResults ?? [])
            );
        }
    }
}
