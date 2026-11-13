using System.Collections.Immutable;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeatureRequirementsRemoved : IEvent;

/// <summary>
/// Removes requirements and every definition-of-done link to them; their numbers stay retired. PlanId null means the feature's own specification.
/// </summary>
public sealed record FeatureRequirementsRemovedV1(
    FeaturePlanId? PlanId,
    ImmutableArray<RequirementId> RequirementIds,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeatureRequirementsRemoved
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (FeatureStateData)stateData;
        var specification = state.SpecificationOf(PlanId);
        specification.RemoveRequirements(RequirementIds);
        SpecificationHistory.Record(state, eventExecutionInfo, MemoryAggregateId);
        return state;
    }
}
