using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using SkillsModule.Domain.Models;

namespace SkillsModule.Domain.Events;

public interface ISkillReferenceAdded : IEvent;

public readonly record struct SkillReferenceAddedV1(
    string RelativePath,
    string Content
) : ISkillReferenceAdded
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (SkillStateData)stateData;

        state.References.TryAdd(
            RelativePath,
            new SkillReference2(Content)
        );

        return state;
    }
}

public readonly record struct SkillReferenceAddedV2(
    string RelativePath,
    string Content,
    bool LoadAutomatically
) : ISkillReferenceAdded
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (SkillStateData)stateData;

        state.References.Add(
            RelativePath,
            new SkillReference2(Content, LoadAutomatically)
        );

        return state;
    }
}
