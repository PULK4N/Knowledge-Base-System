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

        var policies = await Execute(query);

        return policies is null
            ? NotFound()
            : Content(policies, "text/plain");
    }
}
