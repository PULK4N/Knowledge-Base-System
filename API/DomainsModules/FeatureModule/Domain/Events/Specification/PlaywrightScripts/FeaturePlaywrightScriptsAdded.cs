using System.Collections.Immutable;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeaturePlaywrightScriptsAdded : IEvent;

/// <summary>
/// Adds Playwright script paths with the definitions of done they confirm. PlanId null means the feature's own specification.
/// </summary>
public sealed record FeaturePlaywrightScriptsAddedV1(
    FeaturePlanId? PlanId,
    ImmutableArray<PlaywrightScriptEntry> Scripts,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeaturePlaywrightScriptsAdded
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (FeatureStateData)stateData;
        var specification = state.SpecificationOf(PlanId);
        foreach (var entry in Scripts)
            specification.PlaywrightScripts.Add(new PlaywrightScript
            {
                Id = entry.Id,
                Path = entry.Path,
                DefinitionOfDoneIds = entry.DefinitionOfDoneIds.ToList()
            });
        SpecificationHistory.Record(state, eventExecutionInfo, MemoryAggregateId);
        return state;
    }
}
