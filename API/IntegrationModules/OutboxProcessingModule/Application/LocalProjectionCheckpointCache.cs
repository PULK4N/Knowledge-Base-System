using System.Collections.Concurrent;
using EventSourcing.Shared.Models;
using Microsoft.Extensions.Caching.Memory;
using SharedModule.DistributedMessaging.Projections;

namespace OutboxProcessingModule.Application;

public sealed class LocalProjectionCheckpointCache(IMemoryCache cache)
    : IProjectionCheckpointCache
{
    private readonly ConcurrentDictionary<string, Guid> _generations = new(StringComparer.Ordinal);

    public Task<ProjectionCheckpoint> Get(
        string stateMachineId,
        AggregateId aggregateId,
        List<string> projectorNames,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var generation = _generations.GetOrAdd(stateMachineId, _ => Guid.NewGuid());
        var projectors = string.Join(",", projectorNames
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal));
        var key = $"checkPointCache:{stateMachineId}:{aggregateId.Value}:{generation}:{projectors}";
        var orderNumber = cache.TryGetValue(key, out uint cached) ? (uint?)cached : null;
        return Task.FromResult(new ProjectionCheckpoint(key, orderNumber));
    }

    public Task Record(
        ProjectionCheckpoint checkpoint,
        uint orderNumber,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (cache.TryGetValue(checkpoint.Key, out uint current))
            orderNumber = Math.Max(current, orderNumber);

        cache.Set(checkpoint.Key, orderNumber, TimeSpan.FromMinutes(4));

        return Task.CompletedTask;
    }

    public Task Invalidate(string stateMachineId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _generations[stateMachineId] = Guid.NewGuid();
        return Task.CompletedTask;
    }
}
