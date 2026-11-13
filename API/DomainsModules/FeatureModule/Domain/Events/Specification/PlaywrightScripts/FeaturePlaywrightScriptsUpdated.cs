using System.Collections.Immutable;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeaturePlaywrightScriptsUpdated : IEvent;

/// <summary>
/// Replaces script paths and definition-of-done links; snapshots are unchanged. PlanId null means the feature's own specification.
/// </summary>
public sealed record FeaturePlaywrightScriptsUpdatedV1(
    FeaturePlanId? PlanId,
    ImmutableArray<PlaywrightScriptEntry> Scripts,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeaturePlaywrightScriptsUpdated
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (FeatureStateData)stateData;
        var specification = state.SpecificationOf(PlanId);
        foreach (var entry in Scripts)
        {
            var script = specification.PlaywrightScripts.Single(item => item.Id == entry.Id);
            script.Path = entry.Path;
            script.DefinitionOfDoneIds = entry.DefinitionOfDoneIds.ToList();
        }
        SpecificationHistory.Record(state, eventExecutionInfo, MemoryAggregateId);
        return state;
    }
}
