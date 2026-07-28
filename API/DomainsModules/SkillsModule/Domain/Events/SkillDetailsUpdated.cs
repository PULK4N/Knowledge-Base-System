using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace SkillsModule.Domain.Events;

public interface ISkillDetailsUpdated : IEvent;

public sealed class SkillDetailsUpdatedV1 : ISkillDetailsUpdated
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Content { get; init; }
    public List<string> Tags { get; init; } = [];

    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (SkillStateData)stateData;

        state.Name = Name;
        state.Description = Description;
        state.Content = Content;
        state.Tags = [.. Tags];

        return state;
    }
}
