using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using MemoryModule.Domain.Models;

namespace MemoryModule.Domain.Events;

public interface IChatSummaryAdded : IEvent;

public sealed record ChatSummaryAddedV1(
    string Summary
) : IChatSummaryAdded
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (MemoryStateData)stateData;

        state.ChatSummary = new ChatSummary
        {
            Summary = Summary,
            SummaryTimestamp = eventExecutionInfo.Timestamp
        };

        return state;
    }
}
