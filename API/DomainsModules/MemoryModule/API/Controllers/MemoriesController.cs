using System.ComponentModel.DataAnnotations;
using ActionModule.API;
using ActionModule.Shared;
using ActionModule.Shared.Models;
using MemoryModule.Application.DTOs;
using MemoryModule.Application.Queries;
using MemoryModule.API.Requests;
using Microsoft.AspNetCore.Mvc;

namespace MemoryModule.API.Controllers;

[ApiController]
[Route("api/memories")]
public sealed class MemoriesController(
    IExecutorProvider executorProvider
) : ActionController(executorProvider)
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<MemorySummaryDto>>> List(
        [FromServices] SearchMemoriesQuery query,
        [FromQuery] ListMemoriesRequest request
    )
    {
        Map(request, query);

        return Ok(await Execute(query));
    }

    [HttpGet("hybrid-search")]
    public async Task<ActionResult<PagedResult<MemorySummaryDto>>> HybridSearch(
        [FromServices] HybridSearchMemoriesQuery query,
        [FromQuery] HybridSearchMemoriesRequest request
    )
    {
        Map(request, query);

        return Ok(await Execute(query));
    }

    [HttpGet("search")]
    public async Task<ActionResult<MemorySearchQueryResult>> Search(
        [FromQuery, Required] string searchText,
        [FromServices] SearchMemoryQuery query,
        [FromQuery, Range(
            SearchMemoryQuery.MinimumMaximumTokens,
            SearchMemoryQuery.MaximumMaximumTokens
        )]
            int maxTokens = SearchMemoryQuery.DefaultMaximumTokens
    )
    {
        query.SearchText = searchText;
        query.MaxTokens = maxTokens;

        return Ok(await Execute(query));
    }

    [HttpGet("{memoryId:guid}/conversation")]
    public async Task<ActionResult<MemoryConversationDto>> GetConversation(
        Guid memoryId,
        [FromServices] GetMemoryConversationQuery query
    )
    {
        query.MemoryId = memoryId;
        var result = await Execute(query);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{memoryId:guid}/prompts")]
    public async Task<ActionResult<MemoryPromptWindowResult>> GetPromptWindow(
        Guid memoryId,
        [FromServices] GetMemoryPromptWindowQuery query,
        [FromQuery] Guid? promptId = null,
        [FromQuery, Range(0, GetMemoryPromptWindowQuery.MaximumPromptsPerDirection)]
            int before = 1,
        [FromQuery, Range(0, GetMemoryPromptWindowQuery.MaximumPromptsPerDirection)]
            int after = 1,
        [FromQuery, Range(
            GetMemoryPromptWindowQuery.MinimumMaximumTokens,
            GetMemoryPromptWindowQuery.MaximumMaximumTokens
        )]
            int maxTokens = GetMemoryPromptWindowQuery.DefaultMaximumTokens
    )
    {
        query.MemoryId = memoryId;
        query.PromptId = promptId ?? Guid.Empty;
        query.Before = before;
        query.After = after;
        query.MaxTokens = maxTokens;

        var result = await Execute(query);

        return result is null ? NotFound() : Ok(result);
    }

    private static void Map(
        MemorySummarySearchRequest request,
        SearchMemoriesQuery query
    )
    {
        query.Page = request.Page;
        query.PageSize = request.PageSize;
        query.Search = request.Search;
        query.HasSummary = request.HasSummary;
        query.MinimumPromptCount = request.MinimumPromptCount;
        query.SortBy = request.SortBy;
        query.SortDirection = request.SortDirection;
    }

    private static void Map(
        HybridSearchMemoriesRequest request,
        HybridSearchMemoriesQuery query
    )
    {
        query.Page = request.Page;
        query.PageSize = request.PageSize;
        query.Search = request.Query;
        query.HasSummary = request.HasSummary;
        query.MinimumPromptCount = request.MinimumPromptCount;
        query.SortBy = request.SortBy;
        query.SortDirection = request.SortDirection;
    }
}
