using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace FeatureModule.Domain.Events;

public interface IFeatureRemoved : IEvent;

public readonly record struct FeatureRemovedV1 : IFeatureRemoved
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        state.IsDeleted = true;
        return state;
    }
}
