using EventSourcing.Core.Interfaces;
using EventSourcing.Core.Models;
using SkillsModule.Domain.Events;

namespace SkillsModule.Application.Tests;

internal sealed class TestStateMachineDefinitionProvider
    : IStateMachineDefinitionProvider
{
    private readonly StateMachineDefinition _definition =
        new()
        {
            Id = "skills-state-machine",
            InitializationEvents =
            [
                nameof(SkillCreatedV1),
                nameof(SkillCreatedV2)
            ]
        };

    public StateMachineDefinition Get(string stateMachineId) =>
        stateMachineId == _definition.Id
            ? _definition
            : throw new KeyNotFoundException(stateMachineId);

    public IReadOnlyCollection<StateMachineDefinition> GetAll() =>
        [_definition];
}
