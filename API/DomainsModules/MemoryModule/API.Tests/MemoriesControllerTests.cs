using System.ComponentModel.DataAnnotations;
using ActionModule.Shared;
using ActionModule.Shared.Models;
using EventSourcing.Shared.Models;
using MemoryModule.API.Controllers;
using MemoryModule.API.Requests;
using MemoryModule.Application.DTOs;
using MemoryModule.Application.Queries;
using MemoryModule.Persistence.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MemoryModule.API.Tests;

public sealed class MemoriesControllerTests
{
    private static readonly Executor Executor =
        new()
        {
            Id = EventExecutor.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            )
        };

    [Fact]
    public void Requests_use_mode_specific_sort_defaults()
    {
        var list = new ListMemoriesRequest();
        var hybrid = new HybridSearchMemoriesRequest();

        Assert.Equal(MemorySummarySortField.LastActivity, list.SortBy);
        Assert.Equal(SortDirection.Descending, list.SortDirection);
        Assert.Equal(MemorySummarySortField.Relevance, hybrid.SortBy);
        Assert.Equal(SortDirection.Descending, hybrid.SortDirection);
    }

    [Fact]
    public void Hybrid_request_requires_a_bounded_query()
    {
        var request = new HybridSearchMemoriesRequest();
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            validationResults,
            validateAllProperties: true
        );

        Assert.False(isValid);
        Assert.Contains(
            validationResults,
            result => result.MemberNames.Contains(nameof(request.Query))
        );
    }

    [Fact]
    public async Task List_maps_filters_and_sorting_to_the_paged_query()
    {
        var repository = new CapturingMemorySummaryRepository();
        var query = new SearchMemoriesQuery(repository);
        var controller = new MemoriesController(new StubExecutorProvider());
        var request = new ListMemoriesRequest
        {
            Page = 2,
            PageSize = 5,
            Search = "outbox",
            HasSummary = true,
            MinimumPromptCount = 3,
            SortBy = MemorySummarySortField.PromptCount,
            SortDirection = SortDirection.Ascending
        };

        var response = await controller.List(query, request);

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        Assert.IsType<PagedResult<MemorySummaryDto>>(ok.Value);
        var mapped = Assert.IsType<
            EntityQuery<MemorySummaryFilters, MemorySummarySortField>
        >(repository.LastSearchRequest);
        Assert.Equal(new PageRequest(2, 5), mapped.Page);
        Assert.Equal("outbox", mapped.Search);
        Assert.Equal(new MemorySummaryFilters(true, 3), mapped.Filters);
        Assert.Equal(MemorySummarySortField.PromptCount, mapped.Sort.Field);
        Assert.Equal(SortDirection.Ascending, mapped.Sort.Direction);
    }

    [Fact]
    public async Task Hybrid_search_returns_the_same_paged_summary_contract()
    {
        var repository = new CapturingMemorySummaryRepository();
        var memorySearch = new CapturingMemorySearch();
        var query = new HybridSearchMemoriesQuery(memorySearch, repository);
        var controller = new MemoriesController(new StubExecutorProvider());
        var request = new HybridSearchMemoriesRequest
        {
            Page = 1,
            PageSize = 10,
            Query = "what did we decide about replay?",
            HasSummary = false,
            MinimumPromptCount = 2
        };

        var response = await controller.HybridSearch(query, request);

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var result = Assert.IsType<PagedResult<MemorySummaryDto>>(ok.Value);
        Assert.Empty(result.Items);
        Assert.Equal("what did we decide about replay?", memorySearch.LastQuery);
        Assert.False(query.HasSummary);
        Assert.Equal(2, query.MinimumPromptCount);
        Assert.Equal(MemorySummarySortField.Relevance, query.SortBy);
    }

    private sealed class StubExecutorProvider : IExecutorProvider
    {
        public Task<Executor> GetExecutor() => Task.FromResult(Executor);
    }

    private sealed class CapturingMemorySearch : IMemorySearch
    {
        public string? LastQuery { get; private set; }

        public Task<IReadOnlyList<MemorySearchResult>> Search(
            string query,
            HybridMemorySearchOptions? options = null,
            CancellationToken cancellationToken = default
        )
        {
            LastQuery = query;
            return Task.FromResult<IReadOnlyList<MemorySearchResult>>([]);
        }
    }

    private sealed class CapturingMemorySummaryRepository
        : IMemorySummaryRepository
    {
        public object? LastSearchRequest { get; private set; }

        public Task<MemorySummary?> Get(
            AggregateId memoryAggregateId,
            CancellationToken cancellationToken = default
        ) => Task.FromResult<MemorySummary?>(null);

        public Task<List<MemorySummary>> GetMany(
            IReadOnlyCollection<AggregateId> memoryAggregateIds,
            CancellationToken cancellationToken = default
        ) => Task.FromResult<List<MemorySummary>>([]);

        public Task<MemorySummarySearchResult> Search(
            EntityQuery<MemorySummaryFilters, MemorySummarySortField> request,
            CancellationToken cancellationToken = default
        )
        {
            LastSearchRequest = request;
            return Task.FromResult(
                new MemorySummarySearchResult([], 0)
            );
        }

        public Task Write(
            IReadOnlyCollection<AggregateId> memoryAggregateIds,
            IReadOnlyCollection<MemorySummary> summaries,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();
    }
}
