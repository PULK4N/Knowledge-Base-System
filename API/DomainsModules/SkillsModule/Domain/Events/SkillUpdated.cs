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
    public List<SkillReference> References { get; init; } = [];

    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (SkillStateData)stateData;

        state.Skill = CreateSkillDefinition();

        return state;
    }

    private SkillDefinition CreateSkillDefinition()
    {
        return new SkillDefinition
        {
            Name = Name,
            Description = Description,
            Content = Content,
            Tags = [.. Tags],
            References = References
                .Select(reference => new SkillReference
                {
                    RelativePath = reference.RelativePath,
                    Content = reference.Content
                })
                .ToList()
        };
    }
}
