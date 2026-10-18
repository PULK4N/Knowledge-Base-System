using ActionModule.API;
using ActionModule.Shared;
using ActionModule.Shared.Models;
using AdministrationModule.Application.Commands.UniqueConstraints;
using AdministrationModule.Application.DTOs.UniqueConstraints;
using AdministrationModule.Application.Queries.UniqueConstraints;
using Microsoft.AspNetCore.Mvc;

namespace AdministrationModule.API.Controllers;

[ApiController]
[Route("api/administration/unique-constraints")]
public sealed class UniqueConstraintAdministrationController(
    IExecutorProvider executorProvider
) : ActionController(executorProvider)
{
    [HttpGet("state-machines")]
    public async Task<ActionResult<List<string>>> ListStateMachines(
        [FromServices] ListStateMachineIdsQuery query
    ) =>
        Ok(await Execute(query));

    [HttpPost("{stateMachineId}/rebuild")]
    public async Task<ActionResult<UniqueConstraintRebuildResult>> Rebuild(
        string stateMachineId,
        [FromServices] RebuildUniqueConstraintsCommand command
    )
    {
        command.StateMachineId = stateMachineId;

        return Ok(await Execute(command));
    }
}
