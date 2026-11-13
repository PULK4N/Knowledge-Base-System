using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Application.DTOs;
using FeatureModule.Application.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

/// <summary>Saves or removes script snapshots; each saved snapshot is stamped with the write time.</summary>
public sealed class SetFeaturePlaywrightScriptSnapshotsCommand(StateMachineHandler stateMachineHandler)
    : FeatureSpecificationCommand(stateMachineHandler)
{
    public List<PlaywrightScriptSnapshotInput> Snapshots { get; set; } = [];

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(FeatureId != Guid.Empty && Snapshots.Count > 0);

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();
        return await ExecuteEvent(executor, new FeaturePlaywrightScriptSnapshotsSetV1(ScopePlanId,
            [.. Snapshots.Select(input => new PlaywrightScriptSnapshotEntry(PlaywrightScriptId.FromDatabaseGuid(input.Id), input.Content))],
            SessionId, memoryAggregateId), memoryAggregateId);
    }
}
