using System.ComponentModel.DataAnnotations;
using ActionModule.API;
using ActionModule.Shared;
using KnowledgeSearchModule.Application;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeSearchModule.API;

[ApiController]
[Route("api/knowledge/search")]
public sealed class KnowledgeSearchController(
    IExecutorProvider executorProvider
) : ActionController(executorProvider)
{
    [HttpGet]
    public async Task<ActionResult<KnowledgeSearchResultsDto>> Search(
        [FromServices] SearchKnowledgeQuery searchQuery,
        [FromQuery, Required, StringLength(
            SearchKnowledgeQuery.MaximumSearchTextLength,
            MinimumLength = 1
        )]
            string query,
        [FromQuery, Required]
            List<string> keywords,
        [FromQuery, Range(
            SearchKnowledgeQuery.MinimumResultCount,
            SearchKnowledgeQuery.MaximumResultCount
        )]
            int resultCount = SearchKnowledgeQuery.DefaultResultCount
    )
    {
        searchQuery.SearchText = query;
        searchQuery.Keywords = keywords;
        searchQuery.ResultCount = resultCount;

        return Ok(await Execute(searchQuery));
    }
}
