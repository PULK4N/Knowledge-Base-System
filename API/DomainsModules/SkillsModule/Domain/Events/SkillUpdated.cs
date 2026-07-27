using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using SkillsModule.Domain.Models;

namespace SkillsModule.Domain.Events;

public interface ISkillUpdated : IEvent
{
    string Name { get; }
    string Description { get; }
    string Content { get; }
    List<string> Tags { get; }
    Dictionary<string, SkillReference> References { get; }
}

public sealed class SkillUpdatedV1 : ISkillUpdated
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Content { get; init; }
    public List<string> Tags { get; init; } = [];
    public Dictionary<string, SkillReference> References { get; init; } =
        new(StringComparer.Ordinal);

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
        return state;
    }
}
