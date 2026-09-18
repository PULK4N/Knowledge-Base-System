using EventSourcing.Shared.Models;

namespace SharedModule.DistributedMessaging.Projections;

/// <summary>
/// Tracks completed projection snapshots. The captured key keeps an in-flight
/// delivery from restoring a checkpoint invalidated by a manual replay.
/// </summary>
public sealed record ProjectionCheckpoint(string Key, uint? OrderNumber);

public interface IProjectionCheckpointCache
{
    Task<ProjectionCheckpoint> Get(
        string stateMachineId,
        AggregateId aggregateId,
        List<string> projectorNames,
        CancellationToken cancellationToken = default
    );

    Task Record(
        ProjectionCheckpoint checkpoint,
        uint orderNumber,
        CancellationToken cancellationToken = default
    );

    Task Invalidate(
        string stateMachineId,
        CancellationToken cancellationToken = default
    );
}
