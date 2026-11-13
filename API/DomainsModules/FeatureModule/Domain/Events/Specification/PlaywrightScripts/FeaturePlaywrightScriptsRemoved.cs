using System.Collections.Immutable;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeaturePlaywrightScriptsRemoved : IEvent;

/// <summary>
/// Removes Playwright scripts. PlanId null means the feature's own specification.
/// </summary>
public sealed record FeaturePlaywrightScriptsRemovedV1(
    FeaturePlanId? PlanId,
    ImmutableArray<PlaywrightScriptId> ScriptIds,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeaturePlaywrightScriptsRemoved
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (FeatureStateData)stateData;
        var specification = state.SpecificationOf(PlanId);
        specification.PlaywrightScripts.RemoveAll(script => ScriptIds.Contains(script.Id));
        SpecificationHistory.Record(state, eventExecutionInfo, MemoryAggregateId);
        return state;
    }
}
