using ActionModule.API;
using ActionModule.Shared;
using ActionModule.Shared.Models;
using AdministrationModule.Application.Commands;
using AdministrationModule.Application.DTOs;
using AdministrationModule.Application.Queries;
using Microsoft.AspNetCore.Mvc;

namespace AdministrationModule.API.Controllers;

[ApiController]
[Route("api/administration/projections")]
public sealed class AdministrationController(
    IExecutorProvider executorProvider
) : ActionController(executorProvider)
{
    [HttpGet]
    public async Task<ActionResult<List<ProjectionGroupDto>>> List(
        [FromServices] ListProjectionGroupsQuery query
    ) =>
        Ok(await Execute(query));

    [HttpPost("{stateMachineId}/execute")]
    public async Task<ActionResult<ProjectionReplayQueuedResult>> Replay(
        string stateMachineId,
        [FromServices] QueueProjectionReplayCommand command
    )
    {
        command.StateMachineId = stateMachineId;

        return Ok(await Execute(command));
    }
}
