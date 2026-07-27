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
        if (state.References.ContainsKey(RelativePath))
        {
            state.References[RelativePath] = new SkillReference(Content);
        }

        return state;
    }
}
