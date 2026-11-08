using System.Collections.Immutable;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using SkillsModule.Domain.Models;

namespace SkillsModule.Domain.Events;

public interface ISkillUseScenariosSet : IEvent;

public readonly record struct SkillUseScenariosSetV1(
    ImmutableArray<string> Scenarios,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : ISkillUseScenariosSet
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (SkillStateData)stateData;
        state.UseScenarios = Scenarios.ToList();
        state.MemoryHistory.Add(
            new MemoryHistoryRecord(
                eventExecutionInfo.EventName,
                eventExecutionInfo.Timestamp,
                MemoryAggregateId
            )
        );

        return state;
    }
}
