using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using SkillsModule.Domain.Models;

namespace SkillsModule.Domain.Events;

public interface ISkillAttachmentAdded : IEvent;

public readonly record struct SkillAttachmentAddedV1(
    Attachment Attachment
) : ISkillAttachmentAdded
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (SkillStateData)stateData;

        state.Attachments.TryAdd(Attachment.Id, Attachment);

        return state;
    }
}

public interface ISkillAttachmentDeleted : IEvent;

public readonly record struct SkillAttachmentDeletedV1(
    FileId AttachmentId
) : ISkillAttachmentDeleted
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (SkillStateData)stateData;

        state.Attachments.Remove(AttachmentId);

        return state;
    }
}

public readonly record struct SkillAttachmentAddedV2(
    Attachment Attachment,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : ISkillAttachmentAdded
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (SkillStateData)stateData;
        state.Attachments.TryAdd(Attachment.Id, Attachment);
        state.MemoryHistory.Add(
            new MemoryHistoryRecord(
                eventExecutionInfo.EventName,
                eventExecutionInfo.Timestamp,
                MemoryAggregateId
            )
        );
        return state;
    }
}

public readonly record struct SkillAttachmentDeletedV2(
    FileId AttachmentId,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : ISkillAttachmentDeleted
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (SkillStateData)stateData;
        state.Attachments.Remove(AttachmentId);
        state.MemoryHistory.Add(
            new MemoryHistoryRecord(
                eventExecutionInfo.EventName,
                eventExecutionInfo.Timestamp,
                MemoryAggregateId
            )
        );
        return state;
    }
}
