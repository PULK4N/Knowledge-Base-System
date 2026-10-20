using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;

namespace FeatureModule.Application.Commands;

internal static class FeatureCompositeBranchEvents
{
    internal static List<IEvent> CreateOptional(
        string? branchName,
        Guid sessionId,
        AggregateId memoryAggregateId
    ) => branchName is null ? [] : [Create(branchName, sessionId, memoryAggregateId)];

    internal static FeatureCompositeBranchSetV1 Create(
        string branchName,
        Guid sessionId,
        AggregateId memoryAggregateId
    ) => new(
        string.IsNullOrWhiteSpace(branchName) ? null : branchName.Trim(),
        sessionId,
        memoryAggregateId
    );
}
