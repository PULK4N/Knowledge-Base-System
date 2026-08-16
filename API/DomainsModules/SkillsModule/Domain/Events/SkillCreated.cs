using System.Collections.Immutable;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using SkillsModule.Domain.Models;

namespace SkillsModule.Domain.Events;

public interface ISkillCreated : IEvent;

public readonly record struct SkillCreatedV1(
    string Name,
    string Description,
    string Content,
    ImmutableArray<string> Tags,
    ImmutableDictionary<string, SkillReference> References
) : ISkillCreated
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (SkillStateData)stateData;

        state.IsDeleted = false;
        state.Name = Name;
        state.Description = Description;
        state.Content = Content;
        state.Tags = Tags.ToList();
        state.References = References.ToDictionary(
            reference => reference.Key,
            reference => new SkillReference2(reference.Value.Content),
            StringComparer.Ordinal
        );
        return state;
    }
}

public readonly record struct SkillCreatedV2(
    string Name,
    string Description,
    string Content,
    ImmutableArray<string> Tags,
    ImmutableDictionary<string, SkillReference2> References
) : ISkillCreated
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (SkillStateData)stateData;

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
        return state;
    }
}
