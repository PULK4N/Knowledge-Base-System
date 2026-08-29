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
        [FromBody] CreateAgentFamilyCommand command
    ) =>
        Ok((PolicyCommandResult)await Execute(command));

    [HttpPost("update")]
    public async Task<ActionResult<PolicyCommandResult>> Update(
        [FromBody] UpdateAgentFamilyCommand command
    ) =>
        Ok((PolicyCommandResult)await Execute(command));

    [HttpPost("remove")]
    public async Task<ActionResult<PolicyCommandResult>> Remove(
        [FromBody] RemoveAgentFamilyCommand command
    ) =>
        Ok((PolicyCommandResult)await Execute(command));

    [HttpPost("policies")]
    public async Task<ActionResult<PolicyAddedCommandResult>> AddPolicy(
        [FromBody] AddAgentFamilyPolicyCommand command
    ) =>
        Ok(
            (PolicyAddedCommandResult)await Execute(command)
        );

    [HttpPost("policies/update")]
    public async Task<ActionResult<PolicyCommandResult>> UpdatePolicy(
        [FromBody] UpdateAgentFamilyPolicyCommand command
    ) =>
        Ok((PolicyCommandResult)await Execute(command));

    [HttpPost("policies/remove")]
    public async Task<ActionResult<PolicyCommandResult>> RemovePolicy(
        [FromBody] RemoveAgentFamilyPolicyCommand command
    ) =>
        Ok((PolicyCommandResult)await Execute(command));
}
