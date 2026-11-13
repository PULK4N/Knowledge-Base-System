using System.Collections.Immutable;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeatureDefinitionsOfDoneAdded : IEvent;

/// <summary>
/// Adds definitions of done in the Not confirmed state with the numbers the command assigned. PlanId null means the feature's own specification.
/// </summary>
public sealed record FeatureDefinitionsOfDoneAddedV1(
    FeaturePlanId? PlanId,
    ImmutableArray<DefinitionOfDoneEntry> DefinitionsOfDone,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeatureDefinitionsOfDoneAdded
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (FeatureStateData)stateData;
        var specification = state.SpecificationOf(PlanId);
        foreach (var entry in DefinitionsOfDone)
        {
            specification.DefinitionsOfDone.Add(new DefinitionOfDone
            {
                Id = entry.Id,
                Number = entry.Number,
                Description = entry.Description,
                RequirementIds = entry.RequirementIds.ToList(),
                State = DefinitionOfDoneState.NotConfirmed
            });
            specification.UseDefinitionOfDoneNumber(entry.Number);
        }
        SpecificationHistory.Record(state, eventExecutionInfo, MemoryAggregateId);
        return state;
    }
}
