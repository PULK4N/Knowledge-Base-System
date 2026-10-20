using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

/// <summary>
/// Removes a record. The removal event also removes the record from every plan
/// that links it.
/// </summary>
public sealed class RemoveFeatureRecordCommand(
    StateMachineHandler stateMachineHandler
) : ExistingFeatureCommand(stateMachineHandler)
{
    public required Guid RecordId { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            FeatureId != Guid.Empty
            && RecordId != Guid.Empty
        );

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteEvent(
            executor,
            new FeatureRecordRemovedV2(
                FeatureRecordId.FromDatabaseGuid(RecordId),
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}
