using ActionModule.API;
using ActionModule.Shared;
using ActionModule.Shared.Models;
using FeatureModule.API.Requests;
using FeatureModule.Application.Commands;
using FeatureModule.Application.DTOs;
using FeatureModule.Application.Models;
using FeatureModule.Application.Queries;
using Microsoft.AspNetCore.Mvc;

namespace FeatureModule.API.Controllers;

[ApiController]
[Route("api/features")]
public sealed class FeaturesController(
    IExecutorProvider executorProvider
) : ActionController(executorProvider)
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<FeatureSummaryDto>>> List(
        [FromServices] SearchFeaturesQuery query,
        [FromQuery] SearchFeaturesRequest request
    )
    {
        query.Page = request.Page;
        query.PageSize = request.PageSize;
        query.Search = request.Search;
        query.ProjectId = request.ProjectId;
        query.SortBy = request.SortBy;
        query.SortDirection = request.SortDirection;

        return Ok(await Execute(query));
    }

    [HttpPost]
    public async Task<ActionResult<FeatureCreatedCommandResult>> Add(
        [FromBody] AddFeatureCommand command
    )
    {
        var result = (FeatureCreatedCommandResult)await Execute(command);

        return CreatedAtAction(
            nameof(Get),
            new { featureId = result.FeatureId },
            result
        );
    }

    [HttpGet("{featureId:guid}")]
    public async Task<ActionResult<FeatureDto>> Get(
        Guid featureId,
        [FromQuery] uint? orderNumber,
        [FromServices] GetFeatureQuery query
    )
    {
        query.FeatureId = featureId;
        query.OrderNumber = orderNumber ?? 0;

        var feature = await Execute(query);
        return feature is null ? NotFound() : Ok(feature);
    }

    [HttpPost("{featureId:guid}/remove")]
    public async Task<ActionResult<FeatureCommandResult>> Remove(
        Guid featureId,
        [FromServices] RemoveFeatureCommand command
    )
    {
        command.FeatureId = featureId;
        return Ok((FeatureCommandResult)await Execute(command));
    }

    [HttpPost("{featureId:guid}/status")]
    public async Task<ActionResult<FeatureCommandResult>> UpdateStatus(
        Guid featureId,
        [FromBody] UpdateFeatureStatusRequest request,
        [FromServices] UpdateFeatureStatusCommand command
    )
    {
        command.FeatureId = featureId;
        command.Status = request.Status;
        return Ok((FeatureCommandResult)await Execute(command));
    }

    [HttpPost("{featureId:guid}/skills")]
    public async Task<ActionResult<FeatureCommandResult>> AddSkill(
        Guid featureId,
        [FromBody] FeatureSkillRequest request,
        [FromServices] AddFeatureSkillCommand command
    )
    {
        command.FeatureId = featureId;
        command.SkillId = request.SkillId;
        return Ok((FeatureCommandResult)await Execute(command));
    }

    [HttpPost("{featureId:guid}/skills/remove")]
    public async Task<ActionResult<FeatureCommandResult>> RemoveSkill(
        Guid featureId,
        [FromBody] FeatureSkillRequest request,
        [FromServices] RemoveFeatureSkillCommand command
    )
    {
        command.FeatureId = featureId;
        command.SkillId = request.SkillId;
        return Ok((FeatureCommandResult)await Execute(command));
    }

    [HttpPost("{featureId:guid}/records")]
    public async Task<
        ActionResult<FeatureRecordCreatedCommandResult>
    > AddRecord(
        Guid featureId,
        [FromBody] FeatureRecordContentRequest request,
        [FromServices] AddFeatureRecordCommand command
    )
    {
        command.FeatureId = featureId;
        command.UserMessage = request.UserMessage;
        command.AiAnswer = request.AiAnswer;
        return Ok(
            (FeatureRecordCreatedCommandResult)await Execute(command)
        );
    }

    [HttpPost("{featureId:guid}/records/update")]
    public async Task<ActionResult<FeatureCommandResult>> UpdateRecord(
        Guid featureId,
        [FromBody] UpdateFeatureRecordRequest request,
        [FromServices] UpdateFeatureRecordCommand command
    )
    {
        command.FeatureId = featureId;
        command.RecordId = request.RecordId;
        command.UserMessage = request.UserMessage;
        command.AiAnswer = request.AiAnswer;
        return Ok((FeatureCommandResult)await Execute(command));
    }

    [HttpPost("{featureId:guid}/records/remove")]
    public async Task<ActionResult<FeatureCommandResult>> RemoveRecord(
        Guid featureId,
        [FromBody] RemoveFeatureRecordRequest request,
        [FromServices] RemoveFeatureRecordCommand command
    )
    {
        command.FeatureId = featureId;
        command.RecordId = request.RecordId;
        return Ok((FeatureCommandResult)await Execute(command));
    }

    [HttpPost("{featureId:guid}/research-discoveries")]
    public async Task<
        ActionResult<FeatureResearchDiscoveryCreatedCommandResult>
    > AddResearchDiscovery(
        Guid featureId,
        [FromBody] FeatureResearchDiscoveryContentRequest request,
        [FromServices] AddFeatureResearchDiscoveryCommand command
    )
    {
        command.FeatureId = featureId;
        command.Content = request.Content;
        command.SourceType = request.SourceType;
        command.SourceReference = request.SourceReference;
        return Ok(
            (FeatureResearchDiscoveryCreatedCommandResult)await Execute(
                command
            )
        );
    }

    [HttpPost("{featureId:guid}/research-discoveries/update")]
    public async Task<
        ActionResult<FeatureCommandResult>
    > UpdateResearchDiscovery(
        Guid featureId,
        [FromBody] UpdateFeatureResearchDiscoveryRequest request,
        [FromServices] UpdateFeatureResearchDiscoveryCommand command
    )
    {
        command.FeatureId = featureId;
        command.DiscoveryId = request.DiscoveryId;
        command.Content = request.Content;
        command.SourceType = request.SourceType;
        command.SourceReference = request.SourceReference;
        return Ok((FeatureCommandResult)await Execute(command));
    }

    [HttpPost("{featureId:guid}/research-discoveries/remove")]
    public async Task<
        ActionResult<FeatureCommandResult>
    > RemoveResearchDiscovery(
        Guid featureId,
        [FromBody] RemoveFeatureResearchDiscoveryRequest request,
        [FromServices] RemoveFeatureResearchDiscoveryCommand command
    )
    {
        command.FeatureId = featureId;
        command.DiscoveryId = request.DiscoveryId;
        return Ok((FeatureCommandResult)await Execute(command));
    }

    [HttpPost("{featureId:guid}/plans")]
    public async Task<ActionResult<FeaturePlanCreatedCommandResult>> AddPlan(
        Guid featureId,
        [FromBody] FeaturePlanContentRequest request,
        [FromServices] AddFeaturePlanCommand command
    )
    {
        command.FeatureId = featureId;
        command.Title = request.Title;
        command.Content = request.Content;
        command.ContentType = request.ContentType;
        return Ok(
            (FeaturePlanCreatedCommandResult)await Execute(command)
        );
    }

    [HttpPost("{featureId:guid}/plans/current")]
    public async Task<ActionResult<FeatureCommandResult>> UpdateCurrentPlan(
        Guid featureId,
        [FromBody] FeaturePlanContentRequest request,
        [FromServices] UpdateCurrentFeaturePlanCommand command
    )
    {
        command.FeatureId = featureId;
        command.Title = request.Title;
        command.Content = request.Content;
        command.ContentType = request.ContentType;
        return Ok((FeatureCommandResult)await Execute(command));
    }

    [HttpPost("{featureId:guid}/plans/current/change")]
    public async Task<ActionResult<FeatureCommandResult>> ChangeCurrentPlan(
        Guid featureId,
        [FromBody] FeaturePlanRequest request,
        [FromServices] ChangeCurrentFeaturePlanCommand command
    )
    {
        command.FeatureId = featureId;
        command.PlanId = request.PlanId;
        return Ok((FeatureCommandResult)await Execute(command));
    }

    [HttpPost("{featureId:guid}/plans/remove")]
    public async Task<ActionResult<FeatureCommandResult>> RemovePlan(
        Guid featureId,
        [FromBody] FeaturePlanRequest request,
        [FromServices] RemoveFeaturePlanCommand command
    )
    {
        command.FeatureId = featureId;
        command.PlanId = request.PlanId;
        return Ok((FeatureCommandResult)await Execute(command));
    }
}
