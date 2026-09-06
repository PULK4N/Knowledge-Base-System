using EventSourcing.Core.Interfaces;
using EventSourcing.Core.Models;
using EventSourcing.Persistence.Models;
using EventSourcing.Persistence.Serialization;
using EventSourcing.Shared.Exceptions;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace OutboxProcessingModule.Tests;

public sealed class TestDefinitionProvider(params StateMachineDefinition[] definitions)
    : IStateMachineDefinitionProvider
{
    private readonly Dictionary<string, StateMachineDefinition> _definitions =
        definitions.ToDictionary(definition => definition.Id, StringComparer.Ordinal);

    public StateMachineDefinition Get(string stateMachineId) =>
        _definitions.TryGetValue(stateMachineId, out var definition)
            ? definition
            : throw new StateMachineNotRegisteredException(stateMachineId);

    public IReadOnlyCollection<StateMachineDefinition> GetAll() => _definitions.Values;
}

public sealed class RecordingProjector : IProjector
{
    public List<StateInfo> Received { get; } = [ ];

    public Task Update(List<StateInfo> stateInfo)
    {
        Received.AddRange(stateInfo);
        return Task.CompletedTask;
    }
}

public sealed class SecondRecordingProjector : IProjector
{
    public Task Update(List<StateInfo> stateInfo) => Task.CompletedTask;
}

public static class TestData
{
    public static StateMachineDefinition Definition(
        string id,
        List<string>? projections = null,
        Dictionary<string, StateMachineEventDefinition>? events = null
    ) => new()
    {
        Id = id,
        Projections = projections ?? [ ],
        Events = events ?? [ ]
    };

    public static StateMachineEventDefinition Event(
        List<string>? projections = null,
        HashSet<string>? hooks = null
    ) => new()
    {
        Projections = projections ?? [ ],
        Hooks = hooks ?? new HashSet<string>(StringComparer.Ordinal)
    };

    public static EventExecutionInfo ExecutionInfo(
        string stateMachineId,
        string eventName
    ) => new()
    {
        AggregateId = new AggregateId(Guid.NewGuid()),
        EventName = eventName,
        StateMachineId = stateMachineId,
        NewState = "Any",
        Timestamp = DateTime.UtcNow
    };

    public static SerializedPayloadMessage Row(EventExecutionInfo info) => new()
    {
        Id = 1,
        AggregateId = info.AggregateId.Value,
        SerializedEventExecutionInfo = EventJsonSerializer.Serialize(info),
        SerializedEventData = "{}"
    };
}
