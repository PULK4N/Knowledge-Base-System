using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

public sealed class UpdateFeatureRecordCommand(
    StateMachineHandler stateMachineHandler
) : FeatureRecordContentCommand(stateMachineHandler)
{
    public required Guid RecordId { get; set; }

    public override async Task<bool> CanExecute(Executor executor) =>
        RecordId != Guid.Empty && await base.CanExecute(executor);

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteEvent(
            executor,
            new FeatureRecordUpdatedV3(
                FeatureRecordId.FromDatabaseGuid(RecordId),
                NormalizedType,
                Title,
                Content,
                SourceType,
                SourceReference,
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}
