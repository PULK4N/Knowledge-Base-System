using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace SkillsModule.Domain.Events;

public sealed class SkillDetailsUpdated : IEvent
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Content { get; init; }
    public List<string> Tags { get; init; } = [];

    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (SkillStateData)stateData;

        state.Skill.Name = Name;
        state.Skill.Description = Description;
        state.Skill.Content = Content;
        state.Skill.Tags = [.. Tags];

        return state;
    }
}
