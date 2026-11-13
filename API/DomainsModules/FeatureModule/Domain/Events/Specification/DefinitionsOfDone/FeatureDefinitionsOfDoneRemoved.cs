using System.Collections.Immutable;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeatureDefinitionsOfDoneRemoved : IEvent;

/// <summary>
/// Removes definitions of done and every script link to them; their numbers stay retired. PlanId null means the feature's own specification.
/// </summary>
public sealed record FeatureDefinitionsOfDoneRemovedV1(
    FeaturePlanId? PlanId,
    ImmutableArray<DefinitionOfDoneId> DefinitionOfDoneIds,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeatureDefinitionsOfDoneRemoved
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (FeatureStateData)stateData;
        var specification = state.SpecificationOf(PlanId);
        specification.RemoveDefinitionsOfDone(DefinitionOfDoneIds);
        SpecificationHistory.Record(state, eventExecutionInfo, MemoryAggregateId);
        return state;
    }
}
