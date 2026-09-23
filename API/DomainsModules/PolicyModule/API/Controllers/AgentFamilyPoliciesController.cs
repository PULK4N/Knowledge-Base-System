using PolicyModule.API.Requests;
using System.ComponentModel.DataAnnotations;
using ActionModule.API;
using ActionModule.Shared;
using ActionModule.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using PolicyModule.Application.Commands;
using PolicyModule.Application.DTOs;
using PolicyModule.Application.Models;
using PolicyModule.Application.Queries;

namespace PolicyModule.API.Controllers;

[ApiController]
[Route("api/policies/agent-families")]
public sealed class AgentFamilyPoliciesController(
    IExecutorProvider executorProvider
) : ActionController(executorProvider)
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<PolicyAgentFamilySummaryDto>>> List(
        [FromServices] SearchPolicyAgentFamiliesQuery query,
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

    [HttpGet("{agentFamilyName}/policies")]
    public async Task<ActionResult<PagedResult<PolicyDto>>> ListPolicies(
        string agentFamilyName,
        [FromServices] SearchAgentFamilyPoliciesQuery query,
        [FromQuery, Range(Pagination.DefaultPage, Pagination.MaximumPage)]
            int page = Pagination.DefaultPage,
        [FromQuery, Range(1, Pagination.MaximumPageSize)]
            int pageSize = Pagination.DefaultPageSize,
        [FromQuery] string? search = null
    )
    {
        query.AgentFamilyName = agentFamilyName;
        query.Page = page;
        query.PageSize = pageSize;
        query.Search = search;

        var policies = await Execute(query);

        return policies is null ? NotFound() : Ok(policies);
    }

    [HttpPost]
    public async Task<ActionResult<PolicyCommandResult>> Create(
        [FromBody] CreateAgentFamilyRequest request,
        [FromServices] CreateAgentFamilyCommand command
    )
    {
        command.AgentFamilyName = request.AgentFamilyName;
        command.Description = request.Description;
        command.UseUserOrigin();
        return Ok((PolicyCommandResult)await Execute(command));
    }

    [HttpPost("update")]
    public async Task<ActionResult<PolicyCommandResult>> Update(
        [FromBody] UpdateAgentFamilyRequest request,
        [FromServices] UpdateAgentFamilyCommand command
    )
    {
        command.AgentFamilyName = request.AgentFamilyName;
        command.Description = request.Description;
        command.UseUserOrigin();
        return Ok((PolicyCommandResult)await Execute(command));
    }

    [HttpPost("remove")]
    public async Task<ActionResult<PolicyCommandResult>> Remove(
        [FromBody] RemoveAgentFamilyRequest request,
        [FromServices] RemoveAgentFamilyCommand command
    )
    {
        command.AgentFamilyName = request.AgentFamilyName;
        command.UseUserOrigin();
        return Ok((PolicyCommandResult)await Execute(command));
    }

    [HttpPost("policies")]
    public async Task<ActionResult<PolicyAddedCommandResult>> AddPolicy(
        [FromBody] AddAgentFamilyPolicyRequest request,
        [FromServices] AddAgentFamilyPolicyCommand command
    )
    {
        command.AgentFamilyName = request.AgentFamilyName;
        command.Title = request.Title;
        command.Description = request.Description;
        command.UseUserOrigin();
        return Ok(
            (PolicyAddedCommandResult)await Execute(command)
        );
    }

    [HttpPost("policies/update")]
    public async Task<ActionResult<PolicyCommandResult>> UpdatePolicy(
        [FromBody] UpdateAgentFamilyPolicyRequest request,
        [FromServices] UpdateAgentFamilyPolicyCommand command
    )
    {
        command.AgentFamilyName = request.AgentFamilyName;
        command.PolicyId = request.PolicyId;
        command.Title = request.Title;
        command.Description = request.Description;
        command.UseUserOrigin();
        return Ok((PolicyCommandResult)await Execute(command));
    }

    [HttpPost("policies/remove")]
    public async Task<ActionResult<PolicyCommandResult>> RemovePolicy(
        [FromBody] RemoveAgentFamilyPolicyRequest request,
        [FromServices] RemoveAgentFamilyPolicyCommand command
    )
    {
        command.AgentFamilyName = request.AgentFamilyName;
        command.PolicyId = request.PolicyId;
        command.UseUserOrigin();
        return Ok((PolicyCommandResult)await Execute(command));
    }
}
