using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace FeatureModule.Domain.Events;

public interface IFeatureStatusUpdated : IEvent;

public readonly record struct FeatureStatusUpdatedV1(
    string Status
) : IFeatureStatusUpdated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        state.Status = Status;
        return state;
    }
}
