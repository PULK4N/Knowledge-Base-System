using ActionModule.API;
using ActionModule.Shared;
using Microsoft.AspNetCore.Mvc;
using PolicyModule.Application.Commands;
using PolicyModule.Application.DTOs;
using PolicyModule.Application.Models;
using PolicyModule.Application.Queries;

namespace PolicyModule.API.Controllers;

[ApiController]
[Route("api/policies/general")]
public sealed class GeneralPoliciesController(
    IExecutorProvider executorProvider
) : ActionController(executorProvider)
{
    [HttpGet]
    public async Task<ActionResult<List<PolicyDto>>> List(
        [FromServices] ListGeneralPoliciesQuery query
    ) =>
        Ok(await Execute(query));

    [HttpPost]
    public async Task<ActionResult<PolicyAddedCommandResult>> Add(
        [FromBody] AddGeneralPolicyCommand command
    ) =>
        Ok(
            (PolicyAddedCommandResult)await Execute(command)
        );

    [HttpPost("update")]
    public async Task<ActionResult<PolicyCommandResult>> Update(
        [FromBody] UpdateGeneralPolicyCommand command
    ) =>
        Ok((PolicyCommandResult)await Execute(command));

    [HttpPost("remove")]
    public async Task<ActionResult<PolicyCommandResult>> Remove(
        [FromBody] RemoveGeneralPolicyCommand command
    ) =>
        Ok((PolicyCommandResult)await Execute(command));
}
