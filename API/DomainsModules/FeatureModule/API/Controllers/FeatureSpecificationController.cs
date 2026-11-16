using ActionModule.API;
using ActionModule.Shared;
using FeatureModule.API.Requests;
using FeatureModule.Application.Commands;
using FeatureModule.Application.DTOs;
using FeatureModule.Application.Models;
using FeatureModule.Application.Queries;
using Microsoft.AspNetCore.Mvc;

namespace FeatureModule.API.Controllers;

/// <summary>
/// Requirements, definitions of done and scripts. Each route exists
/// for a plan (/plans/{planId}/...) and for the feature itself.
/// </summary>
[ApiController]
[Route("api/features/{featureId:guid}")]
public sealed class FeatureSpecificationController(
    IExecutorProvider executorProvider
) : ActionController(executorProvider)
{
    [HttpPost("requirements")]
    [HttpPost("plans/{planId:guid}/requirements")]
    public Task<ActionResult<FeatureSpecificationItemsCreatedCommandResult>> AddRequirements(
        Guid featureId, Guid? planId, [FromBody] AddRequirementsRequest request,
        [FromServices] AddFeatureRequirementsCommand command) =>
        Run<FeatureSpecificationItemsCreatedCommandResult>(command, featureId, planId,
            () => command.Requirements = request.Requirements);

    [HttpPost("requirements/update")]
    [HttpPost("plans/{planId:guid}/requirements/update")]
    public Task<ActionResult<FeatureCommandResult>> UpdateRequirements(
        Guid featureId, Guid? planId, [FromBody] UpdateRequirementsRequest request,
        [FromServices] UpdateFeatureRequirementsCommand command) =>
        Run<FeatureCommandResult>(command, featureId, planId, () => command.Requirements = request.Requirements);

    [HttpPost("requirements/remove")]
    [HttpPost("plans/{planId:guid}/requirements/remove")]
    public Task<ActionResult<FeatureCommandResult>> RemoveRequirements(
        Guid featureId, Guid? planId, [FromBody] RemoveRequirementsRequest request,
        [FromServices] RemoveFeatureRequirementsCommand command) =>
        Run<FeatureCommandResult>(command, featureId, planId, () => command.RequirementIds = request.RequirementIds);

    [HttpPost("definitions-of-done")]
    [HttpPost("plans/{planId:guid}/definitions-of-done")]
    public Task<ActionResult<FeatureSpecificationItemsCreatedCommandResult>> AddDefinitionsOfDone(
        Guid featureId, Guid? planId, [FromBody] AddDefinitionsOfDoneRequest request,
        [FromServices] AddFeatureDefinitionsOfDoneCommand command) =>
        Run<FeatureSpecificationItemsCreatedCommandResult>(command, featureId, planId,
            () => command.DefinitionsOfDone = request.DefinitionsOfDone);

    [HttpPost("definitions-of-done/update")]
    [HttpPost("plans/{planId:guid}/definitions-of-done/update")]
    public Task<ActionResult<FeatureCommandResult>> UpdateDefinitionsOfDone(
        Guid featureId, Guid? planId, [FromBody] UpdateDefinitionsOfDoneRequest request,
        [FromServices] UpdateFeatureDefinitionsOfDoneCommand command) =>
        Run<FeatureCommandResult>(command, featureId, planId,
            () => command.DefinitionsOfDone = request.DefinitionsOfDone);

    [HttpPost("definitions-of-done/remove")]
    [HttpPost("plans/{planId:guid}/definitions-of-done/remove")]
    public Task<ActionResult<FeatureCommandResult>> RemoveDefinitionsOfDone(
        Guid featureId, Guid? planId, [FromBody] RemoveDefinitionsOfDoneRequest request,
        [FromServices] RemoveFeatureDefinitionsOfDoneCommand command) =>
        Run<FeatureCommandResult>(command, featureId, planId,
            () => command.DefinitionOfDoneIds = request.DefinitionOfDoneIds);

    [HttpPost("definitions-of-done/states")]
    [HttpPost("plans/{planId:guid}/definitions-of-done/states")]
    public Task<ActionResult<FeatureCommandResult>> SetDefinitionOfDoneStates(
        Guid featureId, Guid? planId, [FromBody] SetDefinitionOfDoneStatesRequest request,
        [FromServices] SetFeatureDefinitionOfDoneStatesCommand command) =>
        Run<FeatureCommandResult>(command, featureId, planId, () => command.States = request.States);

    [HttpPost("plans/{planId:guid}/definitions-of-done/move")]
    public Task<ActionResult<FeatureSpecificationItemsMovedCommandResult>> MoveDefinitionsOfDone(
        Guid featureId, Guid planId, [FromBody] MoveDefinitionsOfDoneRequest request,
        [FromServices] MoveFeatureDefinitionsOfDoneCommand command) =>
        Run<FeatureSpecificationItemsMovedCommandResult>(command, featureId, planId, () =>
        {
            command.TargetPlanId = request.TargetPlanId;
            command.DefinitionOfDoneIds = request.DefinitionOfDoneIds;
        });

    [HttpPost("scripts")]
    [HttpPost("plans/{planId:guid}/scripts")]
    public Task<ActionResult<FeatureSpecificationItemsCreatedCommandResult>> AddScripts(
        Guid featureId, Guid? planId, [FromBody] AddScriptsRequest request,
        [FromServices] AddFeatureScriptsCommand command) =>
        Run<FeatureSpecificationItemsCreatedCommandResult>(command, featureId, planId,
            () => command.Scripts = request.Scripts);

    [HttpPost("scripts/update")]
    [HttpPost("plans/{planId:guid}/scripts/update")]
    public Task<ActionResult<FeatureCommandResult>> UpdateScripts(
        Guid featureId, Guid? planId, [FromBody] UpdateScriptsRequest request,
        [FromServices] UpdateFeatureScriptsCommand command) =>
        Run<FeatureCommandResult>(command, featureId, planId, () => command.Scripts = request.Scripts);

    [HttpPost("scripts/remove")]
    [HttpPost("plans/{planId:guid}/scripts/remove")]
    public Task<ActionResult<FeatureCommandResult>> RemoveScripts(
        Guid featureId, Guid? planId, [FromBody] RemoveScriptsRequest request,
        [FromServices] RemoveFeatureScriptsCommand command) =>
        Run<FeatureCommandResult>(command, featureId, planId, () => command.ScriptIds = request.ScriptIds);

    [HttpPost("scripts/snapshots")]
    [HttpPost("plans/{planId:guid}/scripts/snapshots")]
    public Task<ActionResult<FeatureCommandResult>> SetScriptSnapshots(
        Guid featureId, Guid? planId, [FromBody] SetScriptSnapshotsRequest request,
        [FromServices] SetFeatureScriptSnapshotsCommand command) =>
        Run<FeatureCommandResult>(command, featureId, planId, () => command.Snapshots = request.Snapshots);

    [HttpGet("scripts/{scriptId:guid}/snapshot")]
    [HttpGet("plans/{planId:guid}/scripts/{scriptId:guid}/snapshot")]
    public async Task<ActionResult<ScriptSnapshotDto>> GetScriptSnapshot(
        Guid featureId, Guid? planId, Guid scriptId,
        [FromServices] GetFeatureScriptSnapshotQuery query)
    {
        query.FeatureId = featureId;
        query.PlanId = planId;
        query.ScriptId = scriptId;
        var snapshot = await Execute(query);
        return snapshot is null ? NotFound() : Ok(snapshot);
    }

    private async Task<ActionResult<TResult>> Run<TResult>(
        FeatureSpecificationCommand command, Guid featureId, Guid? planId, Action configure)
    {
        command.UseUserOrigin();
        command.FeatureId = featureId;
        command.PlanId = planId;
        configure();
        return Ok((TResult)await Execute(command));
    }
}
