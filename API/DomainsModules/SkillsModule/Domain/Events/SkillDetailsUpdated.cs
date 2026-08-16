using System.Collections.Immutable;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace SkillsModule.Domain.Events;

public interface ISkillDetailsUpdated : IEvent;

public readonly record struct SkillDetailsUpdatedV1(
    string Name,
    string Description,
    string Content,
    ImmutableArray<string> Tags
) : ISkillDetailsUpdated
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (SkillStateData)stateData;

        state.Name = Name;
        state.Description = Description;
        state.Content = Content;
        state.Tags = Tags.ToList();

        return state;
    }
}
