using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using SkillsModule.Domain.Models;

namespace SkillsModule.Domain.Events;

public interface ISkillReferenceUpdated : IEvent
{
    string RelativePath { get; }
    string Content { get; }
}

public sealed class SkillReferenceUpdatedV1 : ISkillReferenceUpdated
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
