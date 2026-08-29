using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace FeatureModule.Domain.Events;

public interface IFeatureParentSet : IEvent;

public readonly record struct FeatureParentSetV1(
    AggregateId? ParentFeatureId
) : IFeatureParentSet
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        state.ParentFeatureId = ParentFeatureId;
        return state;
    }
}
