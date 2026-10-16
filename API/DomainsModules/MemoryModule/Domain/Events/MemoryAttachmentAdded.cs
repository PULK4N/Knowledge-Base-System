using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using MemoryModule.Domain.Models;

namespace MemoryModule.Domain.Events;

public interface IMemoryAttachmentAdded : IEvent;

public sealed record MemoryAttachmentAddedV1(
    PromptId PromptId,
    MemoryAttachment Attachment
) : IMemoryAttachmentAdded
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (MemoryStateData)stateData;

        state.ChatPrompts[PromptId].Attachments.Add(Attachment);

        return state;
    }
}
