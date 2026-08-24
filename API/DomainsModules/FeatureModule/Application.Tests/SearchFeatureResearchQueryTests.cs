using ActionModule.Shared.Models;
using EventSourcing.Shared.Models;
using FeatureModule.Application.DTOs;
using FeatureModule.Application.Queries;
using FeatureModule.Persistence.Interfaces;

namespace FeatureModule.Application.Tests;

public sealed class SearchFeatureResearchQueryTests
{
    private static readonly Executor Executor =
        new()
        {
            Id = EventExecutor.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            )
        };

    [Fact]
    public async Task Execute_returns_hybrid_research_matches()
    {
        var featureId = AggregateId.FromDatabaseGuid(
            Guid.Parse("11111111-1111-1111-1111-111111111111")
        );
        var discoveryId = Guid.Parse(
            "22222222-2222-2222-2222-222222222222"
        );
        var updatedAt = new DateTime(
            2026,
            8,
            24,
            20,
            0,
            0,
            DateTimeKind.Utc
        );
        var search = new FakeFeatureResearchSearch(
            [
                new FeatureResearchSearchResult(
                    new FeatureResearchSearchCandidate(
                        featureId,
                        "Vector search",
                        discoveryId,
                        "PostgreSQL ranking",
                        "Code",
                        "API/PostgreSqlModule",
                        updatedAt,
                        2,
                        "Use HNSW with cosine distance."
                    ),
                    0.031,
                    1,
                    3
                )
            ]
        );
        var query = new SearchFeatureResearchQuery(search)
        {
            SearchText = "semantic PostgreSQL search"
        };

        var results = await query.Execute(Executor);

        Assert.Equal("semantic PostgreSQL search", search.LastQuery);
        Assert.Equal(5, search.LastOptions!.ResultCount);
        Assert.Equal(50, search.LastOptions.CandidateCount);
        Assert.Equal(
            new FeatureResearchSearchMatchDto(
                featureId.Value,
                "Vector search",
                discoveryId,
                "PostgreSQL ranking",
                "Code",
                "API/PostgreSqlModule",
                updatedAt,
                2,
                "Use HNSW with cosine distance.",
                0.031,
                1,
                3
            ),
            Assert.Single(results)
        );
    }

    [Theory]
    [InlineData("", SearchFeatureResearchQuery.DefaultResultCount)]
    [InlineData("query", SearchFeatureResearchQuery.MinimumResultCount - 1)]
    [InlineData("query", SearchFeatureResearchQuery.MaximumResultCount + 1)]
    public async Task CanExecute_rejects_invalid_searches(
        string searchText,
        int resultCount
    )
    {
        var query = new SearchFeatureResearchQuery(
            new FakeFeatureResearchSearch([])
        )
        {
            SearchText = searchText,
            ResultCount = resultCount
        };

        Assert.False(await query.CanExecute(Executor));
    }

    private sealed class FakeFeatureResearchSearch(
        List<FeatureResearchSearchResult> results
    ) : IFeatureResearchSearch
    {
        public string? LastQuery { get; private set; }
        public HybridFeatureResearchSearchOptions? LastOptions { get; private set; }

        public Task<List<FeatureResearchSearchResult>> Search(
            string query,
            HybridFeatureResearchSearchOptions? options = null,
            CancellationToken cancellationToken = default
        )
        {
            LastQuery = query;
            LastOptions = options;
            return Task.FromResult(results);
        }
    }
}
