using EmbeddingModule;
using EventSourcing.Shared.Models;
using KnowledgeSearchModule.Application;
using Xunit;

namespace KnowledgeSearchModule.Application.Tests;

public sealed class SearchKnowledgeQueryTests
{
    [Fact]
    public async Task Execute_maps_the_bounded_global_search_request()
    {
        var search = new FakeSearch();
        var query = new SearchKnowledgeQuery(search)
        {
            SearchText = "database design",
            ResultCount = 12
        };

        var result = await query.Execute(
            new ActionModule.Shared.Models.Executor
            {
                Id = EventExecutor.FromDatabaseGuid(Guid.Empty)
            }
        );

        Assert.Empty(result);
        Assert.Equal("database design", search.Query);
        Assert.Equal(12, search.Options!.ResultCount);
        Assert.Equal(50, search.Options.CandidateCount);
    }

    private sealed class FakeSearch : IKnowledgeSearch
    {
        public string? Query { get; private set; }
        public HybridKnowledgeSearchOptions? Options { get; private set; }

        public Task<List<KnowledgeSearchResult>> Search(string query, HybridKnowledgeSearchOptions? options = null, CancellationToken cancellationToken = default)
        {
            Query = query;
            Options = options;
            return Task.FromResult(new List<KnowledgeSearchResult>());
        }
    }
}
