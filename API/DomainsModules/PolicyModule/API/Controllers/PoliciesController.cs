using ActionModule.API;
using ActionModule.Shared;
using Microsoft.AspNetCore.Mvc;
using PolicyModule.Application.Queries;

namespace PolicyModule.API.Controllers;

[ApiController]
[Route("api/policies")]
public sealed class PoliciesController(
    IExecutorProvider executorProvider
) : ActionController(executorProvider)
{
    [HttpGet]
    [Produces("text/plain")]
    public async Task<ActionResult<string>> GetByRepository(
        [FromQuery] string repositoryPath,
        [FromServices] GetPoliciesByRepositoryQuery query
    )
    {
        query.RepositoryPath = repositoryPath;

        return Content(await Execute(query), "text/plain");
    }
}
