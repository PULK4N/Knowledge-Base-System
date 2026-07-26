using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using SkillsModule.Domain.Models;

namespace SkillsModule.Domain.Events;

public sealed class SkillReferenceUpdated : IEvent
{
    public required string RelativePath { get; init; }
    public required string Content { get; init; }

    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (SkillStateData)stateData;
        var referenceIndex = state.Skill.References.FindIndex(reference =>
            string.Equals(reference.RelativePath, RelativePath, StringComparison.Ordinal)
        );

        if (referenceIndex < 0)
        {
            throw new InvalidOperationException(
                $"A skill reference with relative path '{RelativePath}' does not exist."
            );
        }

        state.Skill.References[referenceIndex] = new SkillReference
        {
            RelativePath = RelativePath,
            Content = Content
        };

        return state;
    }
}
