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
[Route("api/policies/projects")]
public sealed class ProjectPoliciesController(
    IExecutorProvider executorProvider
) : ActionController(executorProvider)
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<PolicyProjectSummaryDto>>> List(
        [FromServices] SearchPolicyProjectsQuery query,
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

    [HttpGet("{projectId:guid}/policies")]
    public async Task<ActionResult<PagedResult<PolicyDto>>> ListPolicies(
        Guid projectId,
        [FromServices] SearchProjectPoliciesQuery query,
        [FromQuery, Range(Pagination.DefaultPage, Pagination.MaximumPage)]
            int page = Pagination.DefaultPage,
        [FromQuery, Range(1, Pagination.MaximumPageSize)]
            int pageSize = Pagination.DefaultPageSize,
        [FromQuery] string? search = null
    )
    {
        query.ProjectId = projectId;
        query.Page = page;
        query.PageSize = pageSize;
        query.Search = search;

        var policies = await Execute(query);

        return policies is null ? NotFound() : Ok(policies);
    }

    [HttpPost]
    public async Task<
        ActionResult<ProjectCreatedCommandResult>
    > Create(
        [FromBody] CreateProjectCommand command
    )
    {
        var result =
            (ProjectCreatedCommandResult)await Execute(
                command
            );

        return CreatedAtAction(
            nameof(ListPolicies),
            new { projectId = result.ProjectId },
            result
        );
    }

    [HttpPost("update")]
    public async Task<ActionResult<PolicyCommandResult>> Update(
        [FromBody] UpdateProjectCommand command
    ) =>
        Ok((PolicyCommandResult)await Execute(command));

    [HttpPost("delete")]
    public async Task<ActionResult<PolicyCommandResult>> Delete(
        [FromBody] DeleteProjectCommand command
    ) =>
        Ok((PolicyCommandResult)await Execute(command));

    [HttpPost("repositories")]
    public async Task<ActionResult<PolicyCommandResult>> AddRepository(
        [FromBody] AddRepositoryToProjectCommand command
    ) =>
        Ok((PolicyCommandResult)await Execute(command));

    [HttpPost("policies")]
    public async Task<ActionResult<PolicyAddedCommandResult>> AddPolicy(
        [FromBody] AddProjectPolicyCommand command
    ) =>
        Ok(
            (PolicyAddedCommandResult)await Execute(command)
        );

    [HttpPost("policies/update")]
    public async Task<ActionResult<PolicyCommandResult>> UpdatePolicy(
        [FromBody] UpdateProjectPolicyCommand command
    ) =>
        Ok((PolicyCommandResult)await Execute(command));

    [HttpPost("policies/remove")]
    public async Task<ActionResult<PolicyCommandResult>> RemovePolicy(
        [FromBody] RemoveProjectPolicyCommand command
    ) =>
        Ok((PolicyCommandResult)await Execute(command));

    [HttpPost("topics")]
    public async Task<ActionResult<PolicyCommandResult>> AddTopic(
        [FromBody] AddTopicRelationToProjectCommand command
    ) =>
        Ok((PolicyCommandResult)await Execute(command));

    [HttpPost("topics/remove")]
    public async Task<ActionResult<PolicyCommandResult>> RemoveTopic(
        [FromBody] RemoveTopicRelationFromProjectCommand command
    ) =>
        Ok((PolicyCommandResult)await Execute(command));
}
