using ActionModule.API;
using ActionModule.Shared;
using IdentityModule.API.Requests;
using IdentityModule.Application.Commands;
using IdentityModule.Application.DTOs;
using IdentityModule.Application.Queries;
using Microsoft.AspNetCore.Mvc;

namespace IdentityModule.API.Controllers;

[ApiController]
[Route("api/agents")]
public sealed class AgentsController(IExecutorProvider executorProvider) : ActionController(executorProvider)
{
    [HttpGet]
    public async Task<ActionResult<List<AgentDto>>> List([FromServices] ListAgentsQuery query) =>
        Ok(await Execute(query));

    [HttpPost]
    public async Task<ActionResult<AgentDto>> Create(
        [FromBody] CreateAgentRequest request,
        [FromServices] CreateAgentCommand command)
    {
        command.UserName = request.UserName;
        command.DisplayName = request.DisplayName;
        command.IsAdministrator = request.IsAdministrator;
        command.Permissions = request.Permissions;
        return Ok(await Execute(command));
    }

    [HttpPut("{agentId:guid}/permissions")]
    public async Task<ActionResult<AgentDto>> SetPermissions(
        Guid agentId,
        [FromBody] SetAgentPermissionsRequest request,
        [FromServices] SetAgentPermissionsCommand command)
    {
        command.AgentId = agentId;
        command.IsAdministrator = request.IsAdministrator;
        command.Permissions = request.Permissions;
        return Ok(await Execute(command));
    }
}
