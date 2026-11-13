using System.Collections.Immutable;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeatureRequirementsUpdated : IEvent;

/// <summary>
/// Replaces requirement values. A changed type comes with the next number of the new type. PlanId null means the feature's own specification.
/// </summary>
public sealed record FeatureRequirementsUpdatedV1(
    FeaturePlanId? PlanId,
    ImmutableArray<RequirementEntry> Requirements,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeatureRequirementsUpdated
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (FeatureStateData)stateData;
        var specification = state.SpecificationOf(PlanId);
        foreach (var entry in Requirements)
        {
            var requirement = specification.Requirements.Single(item => item.Id == entry.Id);
            requirement.Type = entry.Type;
            requirement.Number = entry.Number;
            requirement.Description = entry.Description;
            requirement.ExternalId = entry.ExternalId;
            requirement.Source = entry.Source;
            specification.UseRequirementNumber(entry.Type, entry.Number);
        }
        SpecificationHistory.Record(state, eventExecutionInfo, MemoryAggregateId);
        return state;
    }
}
