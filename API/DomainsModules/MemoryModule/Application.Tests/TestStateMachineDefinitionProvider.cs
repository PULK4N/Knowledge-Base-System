using EventSourcing.Core.Interfaces;
using EventSourcing.Core.Models;
using MemoryModule.Domain.Events;

namespace MemoryModule.Application.Tests;

internal sealed class TestStateMachineDefinitionProvider
    : IStateMachineDefinitionProvider
{
    private readonly Dictionary<string, StateMachineDefinition> _definitions =
        new[]
        {
            new StateMachineDefinition
            {
                Id = Constants.StateMachineIds.Memory,
                InitializationEvents =
                [
                    nameof(CodexPromptHookRecordedV1),
                    nameof(CodexMemoryMigratedV1)
                ]
            },
            new StateMachineDefinition
            {
                Id = Constants.StateMachineIds.SessionAggregateMap,
                InitializationEvents =
                [
                    nameof(SessionAggregateMapAddedV1)
                ]
            }
        }.ToDictionary(definition => definition.Id);

    public StateMachineDefinition Get(string stateMachineId) =>
        _definitions[stateMachineId];

    public IReadOnlyCollection<StateMachineDefinition> GetAll() =>
        _definitions.Values;
}
