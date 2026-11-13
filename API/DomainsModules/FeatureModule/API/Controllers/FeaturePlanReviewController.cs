using ActionModule.API;
using ActionModule.Shared;
using FeatureModule.API.Requests;
using FeatureModule.Application.Commands;
using FeatureModule.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace FeatureModule.API.Controllers;

/// <summary>
/// Review notes and reviewers of one plan. They take the same request bodies
/// as the feature's review notes and reviewers.
/// </summary>
[ApiController]
[Route("api/features/{featureId:guid}/plans/{planId:guid}")]
public sealed class FeaturePlanReviewController(
    IExecutorProvider executorProvider
) : ActionController(executorProvider)
{
    [HttpPost("review-notes")]
    public Task<ActionResult<FeatureReviewNoteCreatedCommandResult>> AddReviewNote(
        Guid featureId, Guid planId, [FromBody] FeatureReviewNoteContentRequest request,
        [FromServices] AddFeaturePlanReviewNoteCommand command) =>
        Run<FeatureReviewNoteCreatedCommandResult>(command, featureId, planId, () =>
        {
            command.Title = request.Title;
            command.Content = request.Content;
        });

    [HttpPost("review-notes/update")]
    public Task<ActionResult<FeatureCommandResult>> UpdateReviewNote(
        Guid featureId, Guid planId, [FromBody] UpdateFeatureReviewNoteRequest request,
        [FromServices] UpdateFeaturePlanReviewNoteCommand command) =>
        Run<FeatureCommandResult>(command, featureId, planId, () =>
        {
            command.ReviewNoteId = request.ReviewNoteId;
            command.Title = request.Title;
            command.Content = request.Content;
        });

    [HttpPost("review-notes/remove")]
    public Task<ActionResult<FeatureCommandResult>> RemoveReviewNote(
        Guid featureId, Guid planId, [FromBody] RemoveFeatureReviewNoteRequest request,
        [FromServices] RemoveFeaturePlanReviewNoteCommand command) =>
        Run<FeatureCommandResult>(command, featureId, planId, () => command.ReviewNoteId = request.ReviewNoteId);

    [HttpPost("reviewers")]
    public Task<ActionResult<FeatureReviewerCreatedCommandResult>> AddReviewer(
        Guid featureId, Guid planId, [FromBody] AddFeatureReviewerRequest request,
        [FromServices] AddFeaturePlanReviewerCommand command) =>
        Run<FeatureReviewerCreatedCommandResult>(command, featureId, planId, () =>
        {
            command.Title = request.Title;
            command.Description = request.Description;
            command.ReviewLevel = request.ReviewLevel;
        });

    [HttpPost("reviewers/update")]
    public Task<ActionResult<FeatureCommandResult>> UpdateReviewer(
        Guid featureId, Guid planId, [FromBody] UpdateFeatureReviewerRequest request,
        [FromServices] UpdateFeaturePlanReviewerCommand command) =>
        Run<FeatureCommandResult>(command, featureId, planId, () =>
        {
            command.ReviewerId = request.ReviewerId;
            command.Title = request.Title;
            command.Description = request.Description;
            command.ReviewLevel = request.ReviewLevel;
        });

    [HttpPost("reviewers/remove")]
    public Task<ActionResult<FeatureCommandResult>> RemoveReviewer(
        Guid featureId, Guid planId, [FromBody] RemoveFeatureReviewerRequest request,
        [FromServices] RemoveFeaturePlanReviewerCommand command) =>
        Run<FeatureCommandResult>(command, featureId, planId, () => command.ReviewerId = request.ReviewerId);

    private async Task<ActionResult<TResult>> Run<TResult>(
        FeaturePlanReviewCommand command, Guid featureId, Guid planId, Action configure)
    {
        command.UseUserOrigin();
        command.FeatureId = featureId;
        command.PlanId = planId;
        configure();
        return Ok((TResult)await Execute(command));
    }
}
