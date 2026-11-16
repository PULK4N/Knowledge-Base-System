using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Application.DTOs;
using FeatureModule.Application.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

/// <summary>Adds scripts, saving a snapshot for those that include code.</summary>
public sealed class AddFeatureScriptsCommand(StateMachineHandler stateMachineHandler)
    : FeatureSpecificationCommand(stateMachineHandler)
{
    public List<ScriptInput> Scripts { get; set; } = [];

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(FeatureId != Guid.Empty && Scripts.Count > 0);

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();
        var scripts = Scripts.Select(input => (Id: ScriptId.New(), Input: input)).ToList();
        var added = new FeatureScriptsAddedV1(ScopePlanId,
            [.. scripts.Select(script => new ScriptEntry(script.Id, script.Input.Path.Trim(),
                [.. (script.Input.DefinitionOfDoneIds ?? []).Select(DefinitionOfDoneId.FromDatabaseGuid)]))],
            SessionId, memoryAggregateId);
        var snapshots = scripts
            .Where(script => script.Input.SnapshotContent is not null)
            .Select(script => new ScriptSnapshotEntry(script.Id, script.Input.SnapshotContent))
            .ToList();

        await ExecuteEvents(executor, added, _ => snapshots.Count == 0
            ? []
            : [new FeatureScriptSnapshotsSetV1(ScopePlanId, [.. snapshots], SessionId, memoryAggregateId)],
            memoryAggregateId);

        return FeatureSpecificationItemsCreatedCommandResult.Ok(scripts
            .Select(script => new CreatedSpecificationItem(script.Id.Value, script.Input.Path.Trim()))
            .ToList());
    }
}
