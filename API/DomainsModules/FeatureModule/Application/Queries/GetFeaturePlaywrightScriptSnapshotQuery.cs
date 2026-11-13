using ActionModule.Shared;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Models;
using FeatureModule.Application.DTOs;
using FeatureModule.Domain;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Queries;

/// <summary>
/// Reads one script's snapshot code, which plan and feature reads leave out.
/// Null when the feature, plan, script or snapshot does not exist.
/// </summary>
public sealed class GetFeaturePlaywrightScriptSnapshotQuery(StateMachineHandler stateMachineHandler)
    : Query<PlaywrightScriptSnapshotDto?>
{
    public required Guid FeatureId { get; set; }

    /// <summary>The plan holding the script, or null for a feature-level script.</summary>
    public Guid? PlanId { get; set; }

    public required Guid ScriptId { get; set; }

    public override Task<bool> IsAuthorized(Executor executor) => Task.FromResult(true);

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(FeatureId != Guid.Empty && ScriptId != Guid.Empty);

    protected override async Task<PlaywrightScriptSnapshotDto?> ExecuteInternal(Executor executor)
    {
        var stateInfo = await stateMachineHandler.GetByAggregateId(AggregateId.FromDatabaseGuid(FeatureId));
        var planId = PlanId is { } id && id != Guid.Empty ? FeaturePlanId.FromDatabaseGuid(id) : (FeaturePlanId?)null;
        var scriptId = PlaywrightScriptId.FromDatabaseGuid(ScriptId);
        var script = (stateInfo?.StateData as FeatureStateData)?.FindSpecification(planId)?
            .PlaywrightScripts.SingleOrDefault(item => item.Id == scriptId);
        return script?.Snapshot is { } snapshot
            ? new PlaywrightScriptSnapshotDto(ScriptId, script.Path, snapshot.Content, snapshot.UpdatedAt)
            : null;
    }
}
