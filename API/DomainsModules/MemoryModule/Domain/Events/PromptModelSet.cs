using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using MemoryModule.Domain.Models;

namespace MemoryModule.Domain.Events;

public interface IPromptModelSet : IEvent;

public sealed record PromptModelSetV1(
    PromptId PromptId,
    string Model
) : IPromptModelSet
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (MemoryStateData)stateData;

        state.ChatPrompts[PromptId].Model = Model;

        return state;
    }
}
