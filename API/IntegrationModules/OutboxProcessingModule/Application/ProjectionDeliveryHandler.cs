using System.Text.Json;
using EventSourcing.Core;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Persistence.Models;
using SharedModule.DistributedMessaging.Consuming;
using SharedModule.DistributedMessaging.Projections;
using SharedModule.DistributedMessaging.Queues;

namespace OutboxProcessingModule.Application;

/// <summary>
/// Rebuilds the aggregate from committed history unless the selected projector
/// set has already completed a snapshot covering the delivered event.
/// </summary>
public sealed class ProjectionDeliveryHandler(
    ProjectionSelector _selector,
    IEventStore _eventStore,
    StateCalculator _calculator,
    IProjectionCheckpointCache _checkpoints
) : IDeliveryHandler
{
    public DeliveryRole Role => DeliveryRole.Projections;

    public async Task Handle(string body, CancellationToken cancellationToken)
    {
        var wireRow = JsonSerializer.Deserialize<SerializedPayloadMessage>(body)
            ?? throw new InvalidOperationException(
                "The delivered body is not an outbox row.");

        // Status, ExecutionAttempts and Version arrive as a send-time snapshot
        // and are never read or saved here.
        var payload = wireRow.Deserialize().Payload;
        var info = payload.EventExecutionInfo;

        var projectors = _selector.Select(info);
        if (projectors.Count == 0)
            return;

        var checkpoint = await _checkpoints.Get(
            info.StateMachineId,
            info.AggregateId,
            projectors.Select(projector => projector.GetType().Name).ToList(),
            cancellationToken
        );
        if (checkpoint.OrderNumber is { } completed && info.OrderNumber <= completed)
            return;

        var histories = await _eventStore.GetEvents([ info.AggregateId ]);

        if (!histories.TryGetValue(info.AggregateId, out var history)
            || history.Count == 0)
            throw new InvalidOperationException(
                $"Aggregate {info.AggregateId.Value} has no committed history, "
                + "so its projections cannot be rebuilt.");

        // Calculating from full history rather than up to the delivered event
        // means a redelivery projects current state, which is what a snapshot
        // projector should hold.
        var state = await _calculator.Calculate(history, [ ]);
        state.LastExecutedPayloads = [ payload ];

        foreach (var projector in projectors)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await projector.Update([ state ]);
        }

        await _checkpoints.Record(checkpoint, state.CurrentOrderNumber, cancellationToken);
    }
}
