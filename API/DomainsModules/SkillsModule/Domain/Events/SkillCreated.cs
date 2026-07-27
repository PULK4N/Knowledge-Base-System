using System.Collections.Immutable;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using SkillsModule.Domain.Models;

namespace SkillsModule.Domain.Events;

public interface ISkillCreated : IEvent;

public sealed record SkillCreatedV1(
    string Name,
    string Description,
    string Content,
    ImmutableArray<string> Tags,
    ImmutableDictionary<string, SkillReference> References,
    ImmutableDictionary<FileId, Attachment> Attachments
) : ISkillCreated
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (SkillStateData)stateData;

        state.Id = eventExecutionInfo.AggregateId;
        state.IsDeleted = false;
        state.Name = Name;
        state.Description = Description;
        state.Content = Content;
        state.Tags = Tags.ToList();
        state.References = References.ToDictionary(
            reference => reference.Key,
            reference => reference.Value,
            StringComparer.Ordinal
        );
        state.Attachments = Attachments.ToDictionary(
            attachment => attachment.Key,
            attachment => attachment.Value
        );

        return state;
    }
}
