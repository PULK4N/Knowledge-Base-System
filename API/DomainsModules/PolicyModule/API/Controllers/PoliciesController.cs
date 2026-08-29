using ActionModule.API;
using ActionModule.Shared;
using Microsoft.AspNetCore.Mvc;
using PolicyModule.Application.Models;
using PolicyModule.Application.Queries;

namespace PolicyModule.API.Controllers;

[ApiController]
[Route("api/policies")]
public sealed class PoliciesController(
    IExecutorProvider executorProvider
) : ActionController(executorProvider)
{
    [HttpGet]
    public async Task<ActionResult<GetPoliciesByRepositoryResult>> GetByRepository(
        [FromQuery] string repositoryPath,
        [FromServices] GetPoliciesByRepositoryQuery query,
        [FromQuery] string? agentFamily = null
    )
    {
        query.RepositoryPath = repositoryPath;
        query.AgentFamily = agentFamily;

        return Ok(await Execute(query));
    }
}
