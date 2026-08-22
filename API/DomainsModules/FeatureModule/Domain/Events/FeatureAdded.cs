using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace FeatureModule.Domain.Events;

public interface IFeatureAdded : IEvent;

public readonly record struct FeatureAddedV1(
    AggregateId ProjectId,
    string Name,
    string Summary,
    string Status
) : IFeatureAdded
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;

        state.IsDeleted = false;
        state.ProjectId = ProjectId;
        state.Name = Name;
        state.Summary = Summary;
        state.Status = Status;

        return state;
    }
}
