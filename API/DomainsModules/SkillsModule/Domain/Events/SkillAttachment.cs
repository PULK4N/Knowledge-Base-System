using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using SkillsModule.Domain.Models;

namespace SkillsModule.Domain.Events;

public interface ISkillAttachmentAdded : IEvent;

public sealed record SkillAttachmentAddedV1(
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

public sealed record SkillAttachmentDeletedV1(
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
