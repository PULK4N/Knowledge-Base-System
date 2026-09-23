using EventSourcing.Core.Interfaces;
using EventSourcing.Core.Models;
using MemoryModule.Application;
using MemoryModule.Domain.Events;
using SkillsModule.Domain.Events;

namespace SkillsModule.Application.Tests;

internal sealed class TestStateMachineDefinitionProvider
    : IStateMachineDefinitionProvider
{
    private readonly Dictionary<string, StateMachineDefinition> _definitions =
        new[]
        {
            new StateMachineDefinition
            {
                Id = "skills-state-machine",
                InitializationEvents =
                [
                    nameof(SkillCreatedV1),
                    nameof(SkillCreatedV2),
                    nameof(SkillCreatedV3)
                ]
            },
            new StateMachineDefinition
            {
                Id = Constants.StateMachineIds.Memory,
                InitializationEvents = [nameof(MemoryRelationAddedV1)]
            },
            new StateMachineDefinition
            {
                Id = Constants.StateMachineIds.SessionAggregateMap,
                InitializationEvents = [nameof(SessionAggregateMapAddedV1)]
            }
        }.ToDictionary(definition => definition.Id);

    public StateMachineDefinition Get(string stateMachineId) =>
        _definitions[stateMachineId];

    public IReadOnlyCollection<StateMachineDefinition> GetAll() =>
        _definitions.Values;
}
