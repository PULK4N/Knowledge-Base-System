using System.Collections.Immutable;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeaturePlaywrightScriptSnapshotsSet : IEvent;

/// <summary>
/// Sets or removes script snapshots, stamped with the event time. PlanId null means the feature's own specification.
/// </summary>
public sealed record FeaturePlaywrightScriptSnapshotsSetV1(
    FeaturePlanId? PlanId,
    ImmutableArray<PlaywrightScriptSnapshotEntry> Snapshots,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeaturePlaywrightScriptSnapshotsSet
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (FeatureStateData)stateData;
        var specification = state.SpecificationOf(PlanId);
        foreach (var entry in Snapshots)
            specification.PlaywrightScripts.Single(item => item.Id == entry.Id).Snapshot =
                entry.Content is null ? null : new ScriptSnapshot(entry.Content, eventExecutionInfo.Timestamp);
        SpecificationHistory.Record(state, eventExecutionInfo, MemoryAggregateId);
        return state;
    }
}
