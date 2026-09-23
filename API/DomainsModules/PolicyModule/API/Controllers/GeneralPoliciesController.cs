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
[Route("api/policies/general")]
public sealed class GeneralPoliciesController(
    IExecutorProvider executorProvider
) : ActionController(executorProvider)
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<PolicyDto>>> List(
        [FromServices] SearchGeneralPoliciesQuery query,
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

    [HttpPost]
    public async Task<ActionResult<PolicyAddedCommandResult>> Add(
        [FromBody] AddGeneralPolicyRequest request,
        [FromServices] AddGeneralPolicyCommand command
    )
    {
        command.Title = request.Title;
        command.Description = request.Description;
        command.UseUserOrigin();
        return Ok(
            (PolicyAddedCommandResult)await Execute(command)
        );
    }

    [HttpPost("update")]
    public async Task<ActionResult<PolicyCommandResult>> Update(
        [FromBody] UpdateGeneralPolicyRequest request,
        [FromServices] UpdateGeneralPolicyCommand command
    )
    {
        command.PolicyId = request.PolicyId;
        command.Title = request.Title;
        command.Description = request.Description;
        command.UseUserOrigin();
        return Ok((PolicyCommandResult)await Execute(command));
    }

    [HttpPost("remove")]
    public async Task<ActionResult<PolicyCommandResult>> Remove(
        [FromBody] RemoveGeneralPolicyRequest request,
        [FromServices] RemoveGeneralPolicyCommand command
    )
    {
        command.PolicyId = request.PolicyId;
        command.UseUserOrigin();
        return Ok((PolicyCommandResult)await Execute(command));
    }
}
