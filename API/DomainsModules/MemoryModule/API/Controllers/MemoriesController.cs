using System.ComponentModel.DataAnnotations;
using ActionModule.API;
using ActionModule.Shared;
using ActionModule.Shared.Models;
using MemoryModule.Application.DTOs;
using MemoryModule.Application.Queries;
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
        [FromQuery, Range(Pagination.DefaultPage, Pagination.MaximumPage)]
            int page = Pagination.DefaultPage,
        [FromQuery, Range(1, Pagination.MaximumPageSize)]
            int pageSize = Pagination.DefaultPageSize,
        [FromQuery] string? search = null
    )
    {
        query.Page = page;
        query.PageSize = pageSize;
        query.Search = search;

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
}
