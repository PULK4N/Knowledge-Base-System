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
