using System.Collections.Immutable;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeatureRequirementsAdded : IEvent;

/// <summary>
/// Adds requirements with the numbers the command assigned. PlanId null means the feature's own specification.
/// </summary>
public sealed record FeatureRequirementsAddedV1(
    FeaturePlanId? PlanId,
    ImmutableArray<RequirementEntry> Requirements,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeatureRequirementsAdded
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (FeatureStateData)stateData;
        var specification = state.SpecificationOf(PlanId);
        foreach (var entry in Requirements)
        {
            specification.Requirements.Add(new Requirement
            {
                Id = entry.Id,
                Type = entry.Type,
                Number = entry.Number,
                Description = entry.Description,
                ExternalId = entry.ExternalId,
                Source = entry.Source
            });
            specification.UseRequirementNumber(entry.Type, entry.Number);
        }
        SpecificationHistory.Record(state, eventExecutionInfo, MemoryAggregateId);
        return state;
    }
}
