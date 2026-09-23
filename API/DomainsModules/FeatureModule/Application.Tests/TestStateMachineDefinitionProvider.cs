using EventSourcing.Core.Interfaces;
using EventSourcing.Core.Models;
using MemoryModule.Application;
using MemoryModule.Domain.Events;
using FeatureModule.Domain.Events;

namespace FeatureModule.Application.Tests;

internal sealed class TestStateMachineDefinitionProvider
    : IStateMachineDefinitionProvider
{
    private readonly Dictionary<string, StateMachineDefinition> _definitions =
        new List<StateMachineDefinition>
        {
            new StateMachineDefinition
            {
                Id = "features-state-machine",
                InitializationEvents =
                [
                    nameof(FeatureAddedV1),
                    nameof(FeatureAddedV2)
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
