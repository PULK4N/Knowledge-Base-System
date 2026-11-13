using System.Collections.Immutable;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeatureDefinitionsOfDoneUpdated : IEvent;

/// <summary>
/// Replaces the description and requirement links of definitions of done. PlanId null means the feature's own specification.
/// </summary>
public sealed record FeatureDefinitionsOfDoneUpdatedV1(
    FeaturePlanId? PlanId,
    ImmutableArray<DefinitionOfDoneChange> DefinitionsOfDone,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeatureDefinitionsOfDoneUpdated
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (FeatureStateData)stateData;
        var specification = state.SpecificationOf(PlanId);
        foreach (var change in DefinitionsOfDone)
        {
            var definitionOfDone = specification.DefinitionsOfDone.Single(item => item.Id == change.Id);
            definitionOfDone.Description = change.Description;
            definitionOfDone.RequirementIds = change.RequirementIds.ToList();
        }
        SpecificationHistory.Record(state, eventExecutionInfo, MemoryAggregateId);
        return state;
    }
}
