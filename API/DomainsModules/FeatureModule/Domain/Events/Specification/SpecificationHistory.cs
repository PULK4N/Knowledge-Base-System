using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

internal static class SpecificationHistory
{
    internal static void Record(
        FeatureStateData state,
        EventExecutionInfo eventExecutionInfo,
        AggregateId memoryAggregateId
    ) =>
        state.MemoryHistory.Add(
            new MemoryHistoryRecord(
                eventExecutionInfo.EventName,
                eventExecutionInfo.Timestamp,
                memoryAggregateId
            )
        );
}
