using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace SettingsModule.Domain.Events;

public interface ISettingsInitialized : IEvent;

public sealed record SettingsInitializedV1(int BatchSize, string TaskMessage) : ISettingsInitialized
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (SettingsStateData)stateData;
        state.IsInitialized = true;
        state.CodeReviewBatchSize = BatchSize;
        state.CodeReviewTaskMessage = TaskMessage;
        state.OrderNumber = eventExecutionInfo.OrderNumber;
        return state;
    }
}
