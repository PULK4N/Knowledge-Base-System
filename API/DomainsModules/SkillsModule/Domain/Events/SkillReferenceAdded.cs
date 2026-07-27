using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using SkillsModule.Domain.Models;

namespace SkillsModule.Domain.Events;

public interface ISkillReferenceAdded : IEvent
{
    string RelativePath { get; }
    string Content { get; }
}

public sealed class SkillReferenceAddedV1 : ISkillReferenceAdded
{
    public required string RelativePath { get; init; }
    public required string Content { get; init; }

    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (SkillStateData)stateData;

        state.References.TryAdd(
            RelativePath,
            new SkillReference(Content)
        );

        return state;
    }
}
