using EventSourcing.Core.Interfaces;
using EventSourcing.Core.Models;
using FeatureModule.Domain.Events;

namespace FeatureModule.Application.Tests;

internal sealed class TestStateMachineDefinitionProvider
    : IStateMachineDefinitionProvider
{
    private readonly StateMachineDefinition _definition =
        new()
        {
            Id = "features-state-machine",
            InitializationEvents = [nameof(FeatureAddedV1)]
        };

    public StateMachineDefinition Get(string stateMachineId) =>
        stateMachineId == _definition.Id
            ? _definition
            : throw new KeyNotFoundException(stateMachineId);

    public IReadOnlyCollection<StateMachineDefinition> GetAll() =>
        [_definition];
}
