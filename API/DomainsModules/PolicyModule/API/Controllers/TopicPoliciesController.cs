using ActionModule.API;
using ActionModule.Shared;
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
    [HttpGet("{topicName}/policies")]
    public async Task<ActionResult<List<PolicyDto>>> ListPolicies(
        string topicName,
        [FromServices] ListTopicPoliciesQuery query
    )
    {
        query.TopicName = topicName;

        var policies = await Execute(query);

        return policies is null ? NotFound() : Ok(policies);
    }

    [HttpPost]
    public async Task<ActionResult<PolicyCommandResult>> Create(
        [FromBody] CreateTopicCommand command
    ) =>
        Ok((PolicyCommandResult)await Execute(command));

    [HttpPost("update")]
    public async Task<ActionResult<PolicyCommandResult>> Update(
        [FromBody] UpdateTopicCommand command
    ) =>
        Ok((PolicyCommandResult)await Execute(command));

    [HttpPost("remove")]
    public async Task<ActionResult<PolicyCommandResult>> Remove(
        [FromBody] RemoveTopicCommand command
    ) =>
        Ok((PolicyCommandResult)await Execute(command));

    [HttpPost("policies")]
    public async Task<ActionResult<PolicyAddedCommandResult>> AddPolicy(
        [FromBody] AddTopicPolicyCommand command
    ) =>
        Ok(
            (PolicyAddedCommandResult)await Execute(command)
        );

    [HttpPost("policies/update")]
    public async Task<ActionResult<PolicyCommandResult>> UpdatePolicy(
        [FromBody] UpdateTopicPolicyCommand command
    ) =>
        Ok((PolicyCommandResult)await Execute(command));

    [HttpPost("policies/remove")]
    public async Task<ActionResult<PolicyCommandResult>> RemovePolicy(
        [FromBody] RemoveTopicPolicyCommand command
    ) =>
        Ok((PolicyCommandResult)await Execute(command));
}
