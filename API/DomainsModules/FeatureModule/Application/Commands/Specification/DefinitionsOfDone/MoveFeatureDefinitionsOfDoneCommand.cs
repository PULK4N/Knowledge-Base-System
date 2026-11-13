using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Application.DTOs;
using FeatureModule.Application.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

/// <summary>
/// Moves definitions of done from PlanId to TargetPlanId, together with the
/// requirements they link and the scripts that link them. Moved items get the
/// next unused numbers of the target plan and are removed from the source plan.
/// </summary>
public sealed class MoveFeatureDefinitionsOfDoneCommand(StateMachineHandler stateMachineHandler)
    : FeatureSpecificationCommand(stateMachineHandler)
{
    public required Guid TargetPlanId { get; set; }

    public List<Guid> DefinitionOfDoneIds { get; set; } = [];

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(FeatureId != Guid.Empty && PlanId is { } planId && planId != Guid.Empty
            && TargetPlanId != Guid.Empty && DefinitionOfDoneIds.Count > 0);

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();
        var sourcePlanId = ScopePlanId!.Value;
        var targetPlanId = FeaturePlanId.FromDatabaseGuid(TargetPlanId);
        var feature = await LoadFeature();
        var source = feature?.FindSpecification(sourcePlanId) ?? new Specification();
        var target = feature?.FindSpecification(targetPlanId) ?? new Specification();

        var moved = DefinitionOfDoneIds.Select(DefinitionOfDoneId.FromDatabaseGuid).Distinct().ToHashSet();
        var definitionsOfDone = source.DefinitionsOfDone
            .Where(item => moved.Contains(item.Id))
            .OrderBy(item => item.Number)
            .ToList();
        var linked = definitionsOfDone.SelectMany(item => item.RequirementIds).ToHashSet();
        var requirements = source.Requirements
            .Where(item => linked.Contains(item.Id))
            .OrderBy(item => item.Type).ThenBy(item => item.Number)
            .ToList();
        var nextRequirementNumbers = new Dictionary<RequirementType, int>();
        var movedRequirements = requirements
            .Select(item => new MovedSpecificationItemEntry<RequirementId>(item.Id,
                nextRequirementNumbers[item.Type] =
                    nextRequirementNumbers.GetValueOrDefault(item.Type, target.LastRequirementNumber(item.Type)) + 1))
            .ToList();
        var movedDefinitionsOfDone = definitionsOfDone
            .Select((item, index) => new MovedSpecificationItemEntry<DefinitionOfDoneId>(item.Id,
                target.LastDefinitionOfDoneNumber + index + 1))
            .ToList();
        var scriptIds = source.PlaywrightScripts
            .Where(item => item.DefinitionOfDoneIds.Any(moved.Contains))
            .Select(item => item.Id)
            .ToList();

        // Unknown IDs are passed through so the validator rejects the move.
        var unknown = moved.Except(definitionsOfDone.Select(item => item.Id))
            .Select(id => new MovedSpecificationItemEntry<DefinitionOfDoneId>(id, 0));

        await ExecuteEvent(executor, new FeatureDefinitionsOfDoneMovedV1(sourcePlanId, targetPlanId,
            [.. movedDefinitionsOfDone.Concat(unknown)], [.. movedRequirements], [.. scriptIds],
            SessionId, memoryAggregateId), memoryAggregateId);

        return FeatureSpecificationItemsMovedCommandResult.Ok(
            requirements.Zip(movedRequirements, (item, entry) => new MovedSpecificationItem(item.Id.Value,
                    SpecificationLabels.Requirement(item.Type, item.Number),
                    SpecificationLabels.Requirement(item.Type, entry.Number)))
                .Concat(definitionsOfDone.Zip(movedDefinitionsOfDone, (item, entry) => new MovedSpecificationItem(
                    item.Id.Value, SpecificationLabels.DefinitionOfDone(item.Number),
                    SpecificationLabels.DefinitionOfDone(entry.Number))))
                .ToList());
    }
}
