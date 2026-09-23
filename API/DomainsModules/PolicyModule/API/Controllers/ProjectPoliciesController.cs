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

    [HttpGet("{projectId:guid}")]
    public async Task<ActionResult<PolicyProjectDetailsDto>> Get(
        Guid projectId,
        [FromServices] GetPolicyProjectQuery query
    )
    {
        query.ProjectId = projectId;

        var project = await Execute(query);

        return project is null ? NotFound() : Ok(project);
    }

    [HttpGet("{projectId:guid}/policies/{policyId:guid}")]
    public async Task<ActionResult<PolicyHistoryDto>> GetPolicy(
        Guid projectId,
        Guid policyId,
        [FromServices] GetPolicyHistoryQuery query
    )
    {
        query.Scope = PolicyScope.Project;
        query.ProjectId = projectId;
        query.PolicyId = policyId;

        var policy = await Execute(query);
        return policy is null ? NotFound() : Ok(policy);
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
        [FromBody] CreateProjectRequest request,
        [FromServices] CreateProjectCommand command
    )
    {
        command.ProjectName = request.ProjectName;
        command.ProjectDescription = request.ProjectDescription;
        command.RepositoryPaths = request.RepositoryPaths;
        command.UseUserOrigin();
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
        [FromBody] UpdateProjectRequest request,
        [FromServices] UpdateProjectCommand command
    )
    {
        command.ProjectId = request.ProjectId;
        command.ProjectName = request.ProjectName;
        command.ProjectDescription = request.ProjectDescription;
        command.UseUserOrigin();
        return Ok((PolicyCommandResult)await Execute(command));
    }

    [HttpPost("delete")]
    public async Task<ActionResult<PolicyCommandResult>> Delete(
        [FromBody] DeleteProjectRequest request,
        [FromServices] DeleteProjectCommand command
    )
    {
        command.ProjectId = request.ProjectId;
        command.UseUserOrigin();
        return Ok((PolicyCommandResult)await Execute(command));
    }

    [HttpPost("repositories")]
    public async Task<ActionResult<PolicyCommandResult>> AddRepository(
        [FromBody] AddRepositoryToProjectRequest request,
        [FromServices] AddRepositoryToProjectCommand command
    )
    {
        command.ProjectId = request.ProjectId;
        command.RepositoryPath = request.RepositoryPath;
        command.UseUserOrigin();
        return Ok((PolicyCommandResult)await Execute(command));
    }

    [HttpPost("policies")]
    public async Task<ActionResult<PolicyAddedCommandResult>> AddPolicy(
        [FromBody] AddProjectPolicyRequest request,
        [FromServices] AddProjectPolicyCommand command
    )
    {
        command.ProjectId = request.ProjectId;
        command.Title = request.Title;
        command.Description = request.Description;
        command.UseUserOrigin();
        return Ok(
            (PolicyAddedCommandResult)await Execute(command)
        );
    }

    [HttpPost("policies/update")]
    public async Task<ActionResult<PolicyCommandResult>> UpdatePolicy(
        [FromBody] UpdateProjectPolicyRequest request,
        [FromServices] UpdateProjectPolicyCommand command
    )
    {
        command.ProjectId = request.ProjectId;
        command.PolicyId = request.PolicyId;
        command.Title = request.Title;
        command.Description = request.Description;
        command.UseUserOrigin();
        return Ok((PolicyCommandResult)await Execute(command));
    }

    [HttpPost("policies/remove")]
    public async Task<ActionResult<PolicyCommandResult>> RemovePolicy(
        [FromBody] RemoveProjectPolicyRequest request,
        [FromServices] RemoveProjectPolicyCommand command
    )
    {
        command.ProjectId = request.ProjectId;
        command.PolicyId = request.PolicyId;
        command.UseUserOrigin();
        return Ok((PolicyCommandResult)await Execute(command));
    }

    [HttpPost("topics")]
    public async Task<ActionResult<PolicyCommandResult>> AddTopic(
        [FromBody] AddTopicRelationToProjectRequest request,
        [FromServices] AddTopicRelationToProjectCommand command
    )
    {
        command.ProjectId = request.ProjectId;
        command.TopicName = request.TopicName;
        command.UseUserOrigin();
        return Ok((PolicyCommandResult)await Execute(command));
    }

    [HttpPost("topics/remove")]
    public async Task<ActionResult<PolicyCommandResult>> RemoveTopic(
        [FromBody] RemoveTopicRelationFromProjectRequest request,
        [FromServices] RemoveTopicRelationFromProjectCommand command
    )
    {
        command.ProjectId = request.ProjectId;
        command.TopicName = request.TopicName;
        command.UseUserOrigin();
        return Ok((PolicyCommandResult)await Execute(command));
    }
}
