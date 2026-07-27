using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using SkillsModule.Domain.Models;

namespace SkillsModule.Domain.Events;

public sealed class SkillUpdated : IEvent
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Content { get; init; }
    public List<string> Tags { get; init; } = [];
    public Dictionary<string, SkillReference> References { get; init; } =
        new(StringComparer.Ordinal);
    public Dictionary<FileId, Attachment> Attachments { get; init; } = [];

    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (SkillStateData)stateData;

        state.Name = Name;
        state.Description = Description;
        state.Content = Content;
        state.Tags = [.. Tags];
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
