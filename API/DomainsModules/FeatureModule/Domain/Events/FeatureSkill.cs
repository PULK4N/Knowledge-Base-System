using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace FeatureModule.Domain.Events;

public interface IFeatureSkillAdded : IEvent;

public readonly record struct FeatureSkillAddedV1(
    AggregateId SkillId
) : IFeatureSkillAdded
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        state.RelatedSkillIds.Add(SkillId);
        return state;
    }
}

public interface IFeatureSkillRemoved : IEvent;

public readonly record struct FeatureSkillRemovedV1(
    AggregateId SkillId
) : IFeatureSkillRemoved
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        state.RelatedSkillIds.Remove(SkillId);
        return state;
    }
}
