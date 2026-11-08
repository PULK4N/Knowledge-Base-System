using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace SettingsModule.Domain.Events;

public interface ICodeReviewSettingsUpdated : IEvent;

public sealed record CodeReviewSettingsUpdatedV1(int BatchSize, string TaskMessage) : ICodeReviewSettingsUpdated
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (SettingsStateData)stateData;
        state.CodeReviewBatchSize = BatchSize;
        state.CodeReviewTaskMessage = TaskMessage;
        state.OrderNumber = eventExecutionInfo.OrderNumber;
        return state;
    }
}
