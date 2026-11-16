using System.Collections.Immutable;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeatureScriptsRemoved : IEvent;

/// <summary>
/// Removes scripts. PlanId null means the feature's own specification.
/// </summary>
public sealed record FeatureScriptsRemovedV1(
    FeaturePlanId? PlanId,
    ImmutableArray<ScriptId> ScriptIds,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeatureScriptsRemoved
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (FeatureStateData)stateData;
        var specification = state.SpecificationOf(PlanId);
        specification.Scripts.RemoveAll(script => ScriptIds.Contains(script.Id));
        SpecificationHistory.Record(state, eventExecutionInfo, MemoryAggregateId);
        return state;
    }
}
