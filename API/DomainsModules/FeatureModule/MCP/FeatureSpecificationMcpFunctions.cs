using System.ComponentModel;
using FeatureModule.Application.Commands;
using FeatureModule.Application.DTOs;
using FeatureModule.Application.Models;
using FeatureModule.Application.Queries;
using Microsoft.Extensions.AI;

namespace FeatureModule.MCP;

/// <summary>
/// Tools for requirements, definitions of done and scripts. With
/// planId they change that plan's specification; without it, the feature's.
/// Every write takes a list so many items are saved in one call.
/// </summary>
internal static class FeatureSpecificationMcpFunctions
{
    private const string PlanIdDescription =
        "The plan whose specification changes; omit it for the feature-wide specification.";

    internal static List<AIFunction> Create() =>
    [
        FeatureMcpFunctions.CreateFunction(
            (Func<IServiceProvider, Guid, Task<SpecificationDto?>>)GetFeatureSpecification,
            "feature_specification_get",
            "Gets the feature-wide specification: requirements, definitions of done and scripts copied from completed plans or added on the feature, each copy with its origin plan and item ID, plus warnings about missing links. A plan's specification comes with feature_plan_get."
        ),
        FeatureMcpFunctions.CreateFunction(
            (Func<IServiceProvider, Guid, List<RequirementInput>, Guid?, Guid?, Guid?, Task<FeatureSpecificationItemsCreatedCommandResult>>)AddRequirements,
            "feature_requirement_add",
            "Adds requirements. type is Business (BR), Functional (FR), NonFunctional (NFR) or Technical (TR); each gets the next unused number of its type, returned as a label such as FR-3. externalId is the ID in an outside system such as Confluence and need not be unique; source is a link or text."
        ),
        FeatureMcpFunctions.CreateFunction(
            (Func<IServiceProvider, Guid, List<RequirementChangeInput>, Guid?, Guid?, Guid?, Task<FeatureCommandResult>>)UpdateRequirements,
            "feature_requirement_update",
            "Updates requirements by ID; omitted values are kept. Changing the type gives the requirement the next number of the new type, and its old number is never reused."
        ),
        FeatureMcpFunctions.CreateFunction(
            (Func<IServiceProvider, Guid, List<Guid>, Guid?, Guid?, Guid?, Task<FeatureCommandResult>>)RemoveRequirements,
            "feature_requirement_remove",
            "Removes requirements and their definition-of-done links. Their numbers are never reused."
        ),
        FeatureMcpFunctions.CreateFunction(
            (Func<IServiceProvider, Guid, List<DefinitionOfDoneInput>, Guid?, Guid?, Guid?, Task<FeatureSpecificationItemsCreatedCommandResult>>)AddDefinitionsOfDone,
            "feature_dod_add",
            "Adds definitions of done: how to confirm work is done, with the IDs of the requirements each fulfils (same plan or feature). Each starts NotConfirmed and gets the next number, returned as DOD-n."
        ),
        FeatureMcpFunctions.CreateFunction(
            (Func<IServiceProvider, Guid, List<DefinitionOfDoneChangeInput>, Guid?, Guid?, Guid?, Task<FeatureCommandResult>>)UpdateDefinitionsOfDone,
            "feature_dod_update",
            "Updates definition-of-done descriptions and requirement links by ID; omitted values are kept and an empty requirementIds list removes every link."
        ),
        FeatureMcpFunctions.CreateFunction(
            (Func<IServiceProvider, Guid, List<Guid>, Guid?, Guid?, Guid?, Task<FeatureCommandResult>>)RemoveDefinitionsOfDone,
            "feature_dod_remove",
            "Removes definitions of done and their script links. Their numbers are never reused."
        ),
        FeatureMcpFunctions.CreateFunction(
            (Func<IServiceProvider, Guid, List<DefinitionOfDoneStateInput>, Guid?, Guid?, Guid?, Task<FeatureCommandResult>>)SetDefinitionOfDoneStates,
            "feature_dod_state_set",
            "Records the state of definitions of done after checking them: NotConfirmed, Confirmed, Failed or Postponed. stateDescription says how it was checked, or why it failed or could not be checked yet. The check time is recorded."
        ),
        FeatureMcpFunctions.CreateFunction(
            (Func<IServiceProvider, Guid, Guid, Guid, List<Guid>, Guid?, Guid?, Task<FeatureSpecificationItemsMovedCommandResult>>)MoveDefinitionsOfDone,
            "feature_dod_move",
            "Moves definitions of done from one plan to another plan of the same feature that is not completed, for example ones that cannot be confirmed in this plan. The requirements they link and the scripts that link them move too. Moved items keep their IDs, get the next numbers of the target plan (returned as previous and new labels) and are removed from the source plan; items left behind lose their links to moved items."
        ),
        FeatureMcpFunctions.CreateFunction(
            (Func<IServiceProvider, Guid, List<ScriptInput>, Guid?, Guid?, Guid?, Task<FeatureSpecificationItemsCreatedCommandResult>>)AddScripts,
            "feature_script_add",
            "Adds script paths with the IDs of the definitions of done each confirms (same plan or feature). Paths need not be unique. snapshotContent optionally saves the script code."
        ),
        FeatureMcpFunctions.CreateFunction(
            (Func<IServiceProvider, Guid, List<ScriptChangeInput>, Guid?, Guid?, Guid?, Task<FeatureCommandResult>>)UpdateScripts,
            "feature_script_update",
            "Updates script paths and definition-of-done links by ID; omitted values are kept and an empty definitionOfDoneIds list removes every link. Snapshots change only with feature_script_snapshot_set."
        ),
        FeatureMcpFunctions.CreateFunction(
            (Func<IServiceProvider, Guid, List<Guid>, Guid?, Guid?, Guid?, Task<FeatureCommandResult>>)RemoveScripts,
            "feature_script_remove",
            "Removes scripts."
        ),
        FeatureMcpFunctions.CreateFunction(
            (Func<IServiceProvider, Guid, List<ScriptSnapshotInput>, Guid?, Guid?, Guid?, Task<FeatureCommandResult>>)SetScriptSnapshots,
            "feature_script_snapshot_set",
            "Saves script code snapshots by script ID, stamped with the save time; null content removes a snapshot."
        ),
        FeatureMcpFunctions.CreateFunction(
            (Func<IServiceProvider, Guid, Guid, Guid?, Task<ScriptSnapshotDto?>>)GetScriptSnapshot,
            "feature_script_snapshot_get",
            "Gets one script's saved code and its save time. Returns null when the script has no snapshot."
        )
    ];

    private static async Task<SpecificationDto?> GetFeatureSpecification(IServiceProvider services, Guid featureId) =>
        (await FeatureMcpFunctions.GetFullFeature(services, featureId, 0))?.Specification;

    private static Task<FeatureSpecificationItemsCreatedCommandResult> AddRequirements(IServiceProvider services,
        Guid featureId, List<RequirementInput> requirements, [Description(PlanIdDescription)] Guid? planId = null,
        Guid? sessionId = null, Guid? memoryAggregateId = null) =>
        Run<AddFeatureRequirementsCommand, FeatureSpecificationItemsCreatedCommandResult>(services, featureId, planId,
            sessionId, memoryAggregateId, command => command.Requirements = requirements);

    private static Task<FeatureCommandResult> UpdateRequirements(IServiceProvider services,
        Guid featureId, List<RequirementChangeInput> requirements, [Description(PlanIdDescription)] Guid? planId = null,
        Guid? sessionId = null, Guid? memoryAggregateId = null) =>
        Run<UpdateFeatureRequirementsCommand, FeatureCommandResult>(services, featureId, planId,
            sessionId, memoryAggregateId, command => command.Requirements = requirements);

    private static Task<FeatureCommandResult> RemoveRequirements(IServiceProvider services,
        Guid featureId, List<Guid> requirementIds, [Description(PlanIdDescription)] Guid? planId = null,
        Guid? sessionId = null, Guid? memoryAggregateId = null) =>
        Run<RemoveFeatureRequirementsCommand, FeatureCommandResult>(services, featureId, planId,
            sessionId, memoryAggregateId, command => command.RequirementIds = requirementIds);

    private static Task<FeatureSpecificationItemsCreatedCommandResult> AddDefinitionsOfDone(IServiceProvider services,
        Guid featureId, List<DefinitionOfDoneInput> definitionsOfDone, [Description(PlanIdDescription)] Guid? planId = null,
        Guid? sessionId = null, Guid? memoryAggregateId = null) =>
        Run<AddFeatureDefinitionsOfDoneCommand, FeatureSpecificationItemsCreatedCommandResult>(services, featureId, planId,
            sessionId, memoryAggregateId, command => command.DefinitionsOfDone = definitionsOfDone);

    private static Task<FeatureCommandResult> UpdateDefinitionsOfDone(IServiceProvider services,
        Guid featureId, List<DefinitionOfDoneChangeInput> definitionsOfDone, [Description(PlanIdDescription)] Guid? planId = null,
        Guid? sessionId = null, Guid? memoryAggregateId = null) =>
        Run<UpdateFeatureDefinitionsOfDoneCommand, FeatureCommandResult>(services, featureId, planId,
            sessionId, memoryAggregateId, command => command.DefinitionsOfDone = definitionsOfDone);

    private static Task<FeatureCommandResult> RemoveDefinitionsOfDone(IServiceProvider services,
        Guid featureId, List<Guid> definitionOfDoneIds, [Description(PlanIdDescription)] Guid? planId = null,
        Guid? sessionId = null, Guid? memoryAggregateId = null) =>
        Run<RemoveFeatureDefinitionsOfDoneCommand, FeatureCommandResult>(services, featureId, planId,
            sessionId, memoryAggregateId, command => command.DefinitionOfDoneIds = definitionOfDoneIds);

    private static Task<FeatureCommandResult> SetDefinitionOfDoneStates(IServiceProvider services,
        Guid featureId, List<DefinitionOfDoneStateInput> states, [Description(PlanIdDescription)] Guid? planId = null,
        Guid? sessionId = null, Guid? memoryAggregateId = null) =>
        Run<SetFeatureDefinitionOfDoneStatesCommand, FeatureCommandResult>(services, featureId, planId,
            sessionId, memoryAggregateId, command => command.States = states);

    private static Task<FeatureSpecificationItemsMovedCommandResult> MoveDefinitionsOfDone(IServiceProvider services,
        Guid featureId, [Description("The plan the definitions of done are on now.")] Guid planId,
        Guid targetPlanId, List<Guid> definitionOfDoneIds, Guid? sessionId = null, Guid? memoryAggregateId = null) =>
        Run<MoveFeatureDefinitionsOfDoneCommand, FeatureSpecificationItemsMovedCommandResult>(services, featureId, planId,
            sessionId, memoryAggregateId, command =>
            {
                command.TargetPlanId = targetPlanId;
                command.DefinitionOfDoneIds = definitionOfDoneIds;
            });

    private static Task<FeatureSpecificationItemsCreatedCommandResult> AddScripts(IServiceProvider services,
        Guid featureId, List<ScriptInput> scripts, [Description(PlanIdDescription)] Guid? planId = null,
        Guid? sessionId = null, Guid? memoryAggregateId = null) =>
        Run<AddFeatureScriptsCommand, FeatureSpecificationItemsCreatedCommandResult>(services, featureId, planId,
            sessionId, memoryAggregateId, command => command.Scripts = scripts);

    private static Task<FeatureCommandResult> UpdateScripts(IServiceProvider services,
        Guid featureId, List<ScriptChangeInput> scripts, [Description(PlanIdDescription)] Guid? planId = null,
        Guid? sessionId = null, Guid? memoryAggregateId = null) =>
        Run<UpdateFeatureScriptsCommand, FeatureCommandResult>(services, featureId, planId,
            sessionId, memoryAggregateId, command => command.Scripts = scripts);

    private static Task<FeatureCommandResult> RemoveScripts(IServiceProvider services,
        Guid featureId, List<Guid> scriptIds, [Description(PlanIdDescription)] Guid? planId = null,
        Guid? sessionId = null, Guid? memoryAggregateId = null) =>
        Run<RemoveFeatureScriptsCommand, FeatureCommandResult>(services, featureId, planId,
            sessionId, memoryAggregateId, command => command.ScriptIds = scriptIds);

    private static Task<FeatureCommandResult> SetScriptSnapshots(IServiceProvider services,
        Guid featureId, List<ScriptSnapshotInput> snapshots, [Description(PlanIdDescription)] Guid? planId = null,
        Guid? sessionId = null, Guid? memoryAggregateId = null) =>
        Run<SetFeatureScriptSnapshotsCommand, FeatureCommandResult>(services, featureId, planId,
            sessionId, memoryAggregateId, command => command.Snapshots = snapshots);

    private static Task<ScriptSnapshotDto?> GetScriptSnapshot(IServiceProvider services,
        Guid featureId, Guid scriptId, [Description("The plan holding the script; omit it for a feature-wide script.")] Guid? planId = null) =>
        FeatureMcpActionExecutor.ExecuteQuery<GetFeatureScriptSnapshotQuery, ScriptSnapshotDto?>(
            services,
            query =>
            {
                query.FeatureId = featureId;
                query.PlanId = planId;
                query.ScriptId = scriptId;
            });

    private static Task<TResult> Run<TCommand, TResult>(IServiceProvider services, Guid featureId, Guid? planId,
        Guid? sessionId, Guid? memoryAggregateId, Action<TCommand> configure)
        where TCommand : FeatureSpecificationCommand =>
        FeatureMcpActionExecutor.ExecuteCommand<TCommand, TResult>(
            services,
            command =>
            {
                command.FeatureId = featureId;
                command.PlanId = planId;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
                configure(command);
            });
}
