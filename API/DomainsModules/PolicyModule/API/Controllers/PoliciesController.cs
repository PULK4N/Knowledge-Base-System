using System.ComponentModel.DataAnnotations;
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
        [FromQuery, Required] string repositoryPath,
        [FromQuery, Required] string agentFamily,
        [FromServices] GetPoliciesByRepositoryQuery query
    )
    {
        query.RepositoryPath = repositoryPath;
        query.AgentFamily = agentFamily;

        return Ok(await Execute(query));
    }
}
