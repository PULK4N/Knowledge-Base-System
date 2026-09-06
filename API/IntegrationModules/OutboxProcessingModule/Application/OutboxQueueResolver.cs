using EventSourcing.Core.Interfaces;
using EventSourcing.Core.Models;
using EventSourcing.Persistence.Models;
using EventSourcing.Persistence.Serialization;
using EventSourcing.Shared.Models;
using SharedModule.DistributedMessaging.Queues;

namespace OutboxProcessingModule.Application;

/// <summary>
/// Decides which queues an outbox row is published to, reading only the stored
/// execution info and the state machine definition. Handler instances and event
/// data are never touched here, so routing costs one deserialization per row.
/// </summary>
public sealed class OutboxQueueResolver(
    IStateMachineDefinitionProvider _definitions
) : IOutboxQueueResolver
{
    public List<OutboxDispatch> Resolve(List<SerializedPayloadMessage> rows) =>
        rows.Select(row => new OutboxDispatch(row, ResolveQueues(row))).ToList();

    public List<string> ResolveQueues(SerializedPayloadMessage row)
    {
        var info = EventJsonSerializer.Deserialize<EventExecutionInfo>(
            row.SerializedEventExecutionInfo
        );

        var definition = _definitions.Get(info.StateMachineId);
        definition.Events.TryGetValue(info.EventName, out var eventDefinition);

        var queues = new List<string>();

        if (HasProjections(definition, eventDefinition))
            queues.Add(QueueNames.For(info.StateMachineId, DeliveryRole.Projections));

        if (eventDefinition is { Hooks.Count: > 0 })
            queues.Add(QueueNames.For(info.StateMachineId, DeliveryRole.Hooks));

        return queues;
    }

    private static bool HasProjections(
        StateMachineDefinition definition,
        StateMachineEventDefinition? eventDefinition
    ) => definition.Projections.Count > 0 || eventDefinition?.Projections.Count > 0;
}
