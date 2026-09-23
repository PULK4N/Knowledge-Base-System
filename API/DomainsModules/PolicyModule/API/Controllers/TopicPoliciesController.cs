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
[Route("api/policies/topics")]
public sealed class TopicPoliciesController(
    IExecutorProvider executorProvider
) : ActionController(executorProvider)
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<PolicyTopicSummaryDto>>> List(
        [FromServices] SearchPolicyTopicsQuery query,
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

    [HttpGet("{topicName}/policies")]
    public async Task<ActionResult<PagedResult<PolicyDto>>> ListPolicies(
        string topicName,
        [FromServices] SearchTopicPoliciesQuery query,
        [FromQuery, Range(Pagination.DefaultPage, Pagination.MaximumPage)]
            int page = Pagination.DefaultPage,
        [FromQuery, Range(1, Pagination.MaximumPageSize)]
            int pageSize = Pagination.DefaultPageSize,
        [FromQuery] string? search = null
    )
    {
        query.TopicName = topicName;
        query.Page = page;
        query.PageSize = pageSize;
        query.Search = search;

        var policies = await Execute(query);

        return policies is null ? NotFound() : Ok(policies);
    }

    [HttpPost]
    public async Task<ActionResult<PolicyCommandResult>> Create(
        [FromBody] CreateTopicRequest request,
        [FromServices] CreateTopicCommand command
    )
    {
        command.TopicName = request.TopicName;
        command.Description = request.Description;
        command.UseUserOrigin();
        return Ok((PolicyCommandResult)await Execute(command));
    }

    [HttpPost("update")]
    public async Task<ActionResult<PolicyCommandResult>> Update(
        [FromBody] UpdateTopicRequest request,
        [FromServices] UpdateTopicCommand command
    )
    {
        command.TopicName = request.TopicName;
        command.Description = request.Description;
        command.UseUserOrigin();
        return Ok((PolicyCommandResult)await Execute(command));
    }

    [HttpPost("remove")]
    public async Task<ActionResult<PolicyCommandResult>> Remove(
        [FromBody] RemoveTopicRequest request,
        [FromServices] RemoveTopicCommand command
    )
    {
        command.TopicName = request.TopicName;
        command.UseUserOrigin();
        return Ok((PolicyCommandResult)await Execute(command));
    }

    [HttpPost("policies")]
    public async Task<ActionResult<PolicyAddedCommandResult>> AddPolicy(
        [FromBody] AddTopicPolicyRequest request,
        [FromServices] AddTopicPolicyCommand command
    )
    {
        command.TopicName = request.TopicName;
        command.Title = request.Title;
        command.Description = request.Description;
        command.UseUserOrigin();
        return Ok(
            (PolicyAddedCommandResult)await Execute(command)
        );
    }

    [HttpPost("policies/update")]
    public async Task<ActionResult<PolicyCommandResult>> UpdatePolicy(
        [FromBody] UpdateTopicPolicyRequest request,
        [FromServices] UpdateTopicPolicyCommand command
    )
    {
        command.TopicName = request.TopicName;
        command.PolicyId = request.PolicyId;
        command.Title = request.Title;
        command.Description = request.Description;
        command.UseUserOrigin();
        return Ok((PolicyCommandResult)await Execute(command));
    }

    [HttpPost("policies/remove")]
    public async Task<ActionResult<PolicyCommandResult>> RemovePolicy(
        [FromBody] RemoveTopicPolicyRequest request,
        [FromServices] RemoveTopicPolicyCommand command
    )
    {
        command.TopicName = request.TopicName;
        command.PolicyId = request.PolicyId;
        command.UseUserOrigin();
        return Ok((PolicyCommandResult)await Execute(command));
    }
}
