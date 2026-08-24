using ActionModule.Shared;
using ActionModule.Shared.Models;
using EmbeddingModule;
using EventSourcing.Shared.Models;
using KnowledgeSearchModule.API;
using KnowledgeSearchModule.Application;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace KnowledgeSearchModule.API.Tests;

public sealed class KnowledgeSearchControllerTests
{
    [Fact]
    public async Task Search_maps_query_and_result_count()
    {
        var search = new FakeSearch();
        var query = new SearchKnowledgeQuery(search)
        {
            SearchText = string.Empty
        };
        var controller = new KnowledgeSearchController(
            new FixedExecutorProvider()
        );

        var response = await controller.Search(query, "postgres", 7);

        Assert.IsType<OkObjectResult>(response.Result);
        Assert.Equal("postgres", search.Query);
        Assert.Equal(7, search.Options!.ResultCount);
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

    private sealed class FixedExecutorProvider : IExecutorProvider
    {
        public Task<Executor> GetExecutor() =>
            Task.FromResult(
                new Executor
                {
                    Id = EventExecutor.FromDatabaseGuid(Guid.Empty)
                }
            );
    }
}
