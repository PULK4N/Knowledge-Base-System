using System.ComponentModel.DataAnnotations;
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

    [HttpGet("research-discoveries/search")]
    public async Task<
        ActionResult<FeatureRecordSearchResultsDto>
    > SearchResearchDiscoveries(
        [FromServices] SearchFeatureRecordsQuery searchQuery,
        [FromQuery, Required] string query,
        [FromQuery, Required] List<string> keywords,
        [FromQuery, Range(
            SearchFeatureRecordsQuery.MinimumResultCount,
            SearchFeatureRecordsQuery.MaximumResultCount
        )]
            int resultCount = SearchFeatureRecordsQuery.DefaultResultCount
    )
    {
        searchQuery.SearchText = query;
        searchQuery.Keywords = keywords;
        searchQuery.ResultCount = resultCount;

        return Ok(await Execute(searchQuery));
    }

    [HttpPost]
    public async Task<ActionResult<FeatureCreatedCommandResult>> Add(
        [FromBody] AddFeatureRequest request,
        [FromServices] AddFeatureCommand command
    )
    {
        command.ProjectId = request.ProjectId;
        command.Name = request.Name;
        command.Summary = request.Summary;
        command.Status = request.Status;
        command.CompositeBranchName = request.CompositeBranchName;
        command.UseUserOrigin();
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
        command.UseUserOrigin();
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
        command.UseUserOrigin();
        command.FeatureId = featureId;
        command.Status = request.Status;
        command.CompositeBranchName = request.CompositeBranchName;
        return Ok((FeatureCommandResult)await Execute(command));
    }

    [HttpPost("{featureId:guid}/summary")]
    public async Task<ActionResult<FeatureCommandResult>> UpdateSummary(
        Guid featureId,
        [FromBody] UpdateFeatureSummaryRequest request,
        [FromServices] UpdateFeatureSummaryCommand command
    )
    {
        command.UseUserOrigin();
        command.FeatureId = featureId;
        command.Summary = request.Summary;
        command.CompositeBranchName = request.CompositeBranchName;
        return Ok((FeatureCommandResult)await Execute(command));
    }

    [HttpPost("{featureId:guid}/skills")]
    public async Task<ActionResult<FeatureCommandResult>> AddSkill(
        Guid featureId,
        [FromBody] FeatureSkillRequest request,
        [FromServices] AddFeatureSkillCommand command
    )
    {
        command.UseUserOrigin();
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
        command.UseUserOrigin();
        command.FeatureId = featureId;
        command.SkillId = request.SkillId;
        return Ok((FeatureCommandResult)await Execute(command));
    }

    [HttpPost("{featureId:guid}/records")]
    public async Task<
        ActionResult<FeatureRecordCreatedCommandResult>
    > AddRecord(
        Guid featureId,
        [FromBody] AddFeatureRecordRequest request,
        [FromServices] AddFeatureRecordCommand command
    )
    {
        command.UseUserOrigin();
        command.FeatureId = featureId;
        MapRecordContent(request, command);
        command.PlanIds = request.PlanIds;
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
        command.UseUserOrigin();
        command.FeatureId = featureId;
        command.RecordId = request.RecordId;
        MapRecordContent(request, command);
        return Ok((FeatureCommandResult)await Execute(command));
    }

    [HttpPost("{featureId:guid}/records/remove")]
    public async Task<ActionResult<FeatureCommandResult>> RemoveRecord(
        Guid featureId,
        [FromBody] RemoveFeatureRecordRequest request,
        [FromServices] RemoveFeatureRecordCommand command
    )
    {
        command.UseUserOrigin();
        command.FeatureId = featureId;
        command.RecordId = request.RecordId;
        return Ok((FeatureCommandResult)await Execute(command));
    }

    [HttpPost("{featureId:guid}/review-notes")]
    public async Task<
        ActionResult<FeatureReviewNoteCreatedCommandResult>
    > AddReviewNote(
        Guid featureId,
        [FromBody] FeatureReviewNoteContentRequest request,
        [FromServices] AddFeatureReviewNoteCommand command
    )
    {
        command.UseUserOrigin();
        command.FeatureId = featureId;
        command.Title = request.Title;
        command.Content = request.Content;
        return Ok(
            (FeatureReviewNoteCreatedCommandResult)await Execute(command)
        );
    }

    [HttpPost("{featureId:guid}/review-notes/update")]
    public async Task<ActionResult<FeatureCommandResult>> UpdateReviewNote(
        Guid featureId,
        [FromBody] UpdateFeatureReviewNoteRequest request,
        [FromServices] UpdateFeatureReviewNoteCommand command
    )
    {
        command.UseUserOrigin();
        command.FeatureId = featureId;
        command.ReviewNoteId = request.ReviewNoteId;
        command.Title = request.Title;
        command.Content = request.Content;
        return Ok((FeatureCommandResult)await Execute(command));
    }

    [HttpPost("{featureId:guid}/review-notes/remove")]
    public async Task<ActionResult<FeatureCommandResult>> RemoveReviewNote(
        Guid featureId,
        [FromBody] RemoveFeatureReviewNoteRequest request,
        [FromServices] RemoveFeatureReviewNoteCommand command
    )
    {
        command.UseUserOrigin();
        command.FeatureId = featureId;
        command.ReviewNoteId = request.ReviewNoteId;
        return Ok((FeatureCommandResult)await Execute(command));
    }

    [HttpPost("{featureId:guid}/plans")]
    public async Task<ActionResult<FeaturePlanCreatedCommandResult>> AddPlan(
        Guid featureId,
        [FromBody] AddFeaturePlanRequest request,
        [FromServices] AddFeaturePlanCommand command
    )
    {
        command.UseUserOrigin();
        command.FeatureId = featureId;
        command.BranchName = request.BranchName;
        command.BranchDescription = request.BranchDescription;
        command.PreviousPlanId = request.PreviousPlanId;
        command.BranchSources = request.BranchSources;
        MapPlanContent(request, command);
        return Ok(
            (FeaturePlanCreatedCommandResult)await Execute(command)
        );
    }

    [HttpPost("{featureId:guid}/plans/{planId:guid}/update")]
    public async Task<ActionResult<FeatureCommandResult>> UpdatePlan(
        Guid featureId,
        Guid planId,
        [FromBody] FeaturePlanContentRequest request,
        [FromServices] UpdateFeaturePlanCommand command
    )
    {
        command.UseUserOrigin();
        command.FeatureId = featureId;
        command.PlanId = planId;
        MapPlanContent(request, command);
        return Ok((FeatureCommandResult)await Execute(command));
    }

    [HttpPost("{featureId:guid}/plans/{planId:guid}/status")]
    public async Task<ActionResult<FeatureCommandResult>> ChangePlanStatus(
        Guid featureId,
        Guid planId,
        [FromBody] ChangeFeaturePlanStatusRequest request,
        [FromServices] ChangeFeaturePlanStatusCommand command
    )
    {
        command.UseUserOrigin();
        command.FeatureId = featureId;
        command.PlanId = planId;
        command.Status = request.Status;
        command.MergedInto = request.MergedInto;
        return Ok((FeatureCommandResult)await Execute(command));
    }

    [HttpPost("{featureId:guid}/plans/{planId:guid}/remove")]
    public async Task<ActionResult<FeatureCommandResult>> RemovePlan(
        Guid featureId,
        Guid planId,
        [FromServices] RemoveFeaturePlanCommand command
    )
    {
        command.UseUserOrigin();
        command.FeatureId = featureId;
        command.PlanId = planId;
        return Ok((FeatureCommandResult)await Execute(command));
    }

    [HttpPost("{featureId:guid}/branch")]
    public async Task<ActionResult<FeatureCommandResult>> SetBranch(
        Guid featureId,
        [FromBody] SetFeatureBranchRequest request,
        [FromServices] SetFeatureBranchCommand command
    )
    {
        command.UseUserOrigin();
        command.FeatureId = featureId;
        command.BranchName = request.BranchName;
        command.Description = request.Description;
        return Ok((FeatureCommandResult)await Execute(command));
    }

    [HttpPost("{featureId:guid}/composite-branch")]
    public async Task<ActionResult<FeatureCommandResult>> SetCompositeBranch(
        Guid featureId,
        [FromBody] SetFeatureCompositeBranchRequest request,
        [FromServices] SetFeatureCompositeBranchCommand command
    )
    {
        command.UseUserOrigin();
        command.FeatureId = featureId;
        command.BranchName = request.BranchName;
        return Ok((FeatureCommandResult)await Execute(command));
    }

    [HttpPost("{featureId:guid}/plans/{planId:guid}/previous-plan")]
    public async Task<ActionResult<FeatureCommandResult>> SetPlanPreviousPlan(
        Guid featureId,
        Guid planId,
        [FromBody] SetFeaturePlanPreviousPlanRequest request,
        [FromServices] SetFeaturePlanPreviousPlanCommand command
    )
    {
        command.UseUserOrigin();
        command.FeatureId = featureId;
        command.PlanId = planId;
        command.PreviousPlanId = request.PreviousPlanId;
        return Ok((FeatureCommandResult)await Execute(command));
    }

    [HttpPost("{featureId:guid}/plans/{planId:guid}/branches")]
    public async Task<ActionResult<FeatureCommandResult>> AddPlanBranch(
        Guid featureId,
        Guid planId,
        [FromBody] AddFeaturePlanBranchRequest request,
        [FromServices] AddFeaturePlanBranchCommand command
    )
    {
        command.UseUserOrigin();
        command.FeatureId = featureId;
        command.PlanId = planId;
        command.BranchName = request.BranchName;
        command.Description = request.Description;
        command.ParentBranchName = request.ParentBranchName;
        command.Sources = request.Sources;
        return Ok((FeatureCommandResult)await Execute(command));
    }

    [HttpPost("{featureId:guid}/plans/{planId:guid}/branches/sources")]
    public async Task<ActionResult<FeatureCommandResult>> SetPlanBranchSources(
        Guid featureId,
        Guid planId,
        [FromBody] SetFeaturePlanBranchSourcesRequest request,
        [FromServices] SetFeaturePlanBranchSourcesCommand command
    )
    {
        command.UseUserOrigin();
        command.FeatureId = featureId;
        command.PlanId = planId;
        command.BranchName = request.BranchName;
        command.Sources = request.Sources;
        return Ok((FeatureCommandResult)await Execute(command));
    }

    [HttpPost("{featureId:guid}/plans/{planId:guid}/branches/update")]
    public async Task<ActionResult<FeatureCommandResult>> UpdatePlanBranch(
        Guid featureId,
        Guid planId,
        [FromBody] UpdateFeaturePlanBranchRequest request,
        [FromServices] UpdateFeaturePlanBranchCommand command
    )
    {
        command.UseUserOrigin();
        command.FeatureId = featureId;
        command.PlanId = planId;
        command.BranchName = request.BranchName;
        command.NewBranchName = request.NewBranchName;
        command.Description = request.Description;
        return Ok((FeatureCommandResult)await Execute(command));
    }

    [HttpPost("{featureId:guid}/plans/{planId:guid}/branches/remove")]
    public async Task<ActionResult<FeatureCommandResult>> RemovePlanBranch(
        Guid featureId,
        Guid planId,
        [FromBody] RemoveFeaturePlanBranchRequest request,
        [FromServices] RemoveFeaturePlanBranchCommand command
    )
    {
        command.UseUserOrigin();
        command.FeatureId = featureId;
        command.PlanId = planId;
        command.BranchName = request.BranchName;
        return Ok((FeatureCommandResult)await Execute(command));
    }

    private static void MapRecordContent(
        FeatureRecordContentRequest request,
        FeatureRecordContentCommand command
    )
    {
        command.Type = request.Type;
        command.Title = request.Title;
        command.Content = request.Content;
        command.SourceType = request.SourceType;
        command.SourceReference = request.SourceReference;
    }

    private static void MapPlanContent(
        FeaturePlanContentRequest request,
        FeaturePlanContentCommand command
    )
    {
        command.Title = request.Title;
        command.Content = request.Content;
        command.ContentType = request.ContentType;
        command.AdditionalInfo = request.AdditionalInfo;
        command.Status = request.Status;
        command.RecordIds = request.RecordIds;
        command.MergedInto = request.MergedInto;
    }
}
