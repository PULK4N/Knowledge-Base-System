using System.Collections.Immutable;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeatureDefinitionsOfDoneStateSet : IEvent;

/// <summary>
/// Records the checked state of definitions of done at the event time. PlanId null means the feature's own specification.
/// </summary>
public sealed record FeatureDefinitionsOfDoneStateSetV1(
    FeaturePlanId? PlanId,
    ImmutableArray<DefinitionOfDoneStateEntry> States,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeatureDefinitionsOfDoneStateSet
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (FeatureStateData)stateData;
        var specification = state.SpecificationOf(PlanId);
        foreach (var entry in States)
        {
            var definitionOfDone = specification.DefinitionsOfDone.Single(item => item.Id == entry.Id);
            definitionOfDone.State = entry.State;
            definitionOfDone.StateDescription = entry.StateDescription;
            definitionOfDone.StateCheckedAt = eventExecutionInfo.Timestamp;
        }
        SpecificationHistory.Record(state, eventExecutionInfo, MemoryAggregateId);
        return state;
    }
}
