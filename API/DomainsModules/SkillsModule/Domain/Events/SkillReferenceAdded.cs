using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using SkillsModule.Domain.Models;

namespace SkillsModule.Domain.Events;

public sealed class SkillReferenceAdded : IEvent
{
    public required string RelativePath { get; init; }
    public required string Content { get; init; }

    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (SkillStateData)stateData;

        if (
            state.Skill.References.Any(reference =>
                string.Equals(reference.RelativePath, RelativePath, StringComparison.Ordinal)
            )
        )
        {
            throw new InvalidOperationException(
                $"A skill reference with relative path '{RelativePath}' already exists."
            );
        }

        state.Skill.References.Add(
            new SkillReference
            {
                RelativePath = RelativePath,
                Content = Content
            }
        );

        return state;
    }
}
