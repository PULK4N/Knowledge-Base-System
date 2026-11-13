using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Application.DTOs;
using FeatureModule.Application.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

/// <summary>Adds Playwright scripts, saving a snapshot for those that include code.</summary>
public sealed class AddFeaturePlaywrightScriptsCommand(StateMachineHandler stateMachineHandler)
    : FeatureSpecificationCommand(stateMachineHandler)
{
    public List<PlaywrightScriptInput> Scripts { get; set; } = [];

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(FeatureId != Guid.Empty && Scripts.Count > 0);

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();
        var scripts = Scripts.Select(input => (Id: PlaywrightScriptId.New(), Input: input)).ToList();
        var added = new FeaturePlaywrightScriptsAddedV1(ScopePlanId,
            [.. scripts.Select(script => new PlaywrightScriptEntry(script.Id, script.Input.Path.Trim(),
                [.. (script.Input.DefinitionOfDoneIds ?? []).Select(DefinitionOfDoneId.FromDatabaseGuid)]))],
            SessionId, memoryAggregateId);
        var snapshots = scripts
            .Where(script => script.Input.SnapshotContent is not null)
            .Select(script => new PlaywrightScriptSnapshotEntry(script.Id, script.Input.SnapshotContent))
            .ToList();

        await ExecuteEvents(executor, added, _ => snapshots.Count == 0
            ? []
            : [new FeaturePlaywrightScriptSnapshotsSetV1(ScopePlanId, [.. snapshots], SessionId, memoryAggregateId)],
            memoryAggregateId);

        return FeatureSpecificationItemsCreatedCommandResult.Ok(scripts
            .Select(script => new CreatedSpecificationItem(script.Id.Value, script.Input.Path.Trim()))
            .ToList());
    }
}
