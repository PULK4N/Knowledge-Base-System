using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace FeatureModule.Domain.Events;

public interface IFeatureSummaryUpdated : IEvent;

public readonly record struct FeatureSummaryUpdatedV1(
    string Summary
) : IFeatureSummaryUpdated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        state.Summary = Summary;
        return state;
    }
}
