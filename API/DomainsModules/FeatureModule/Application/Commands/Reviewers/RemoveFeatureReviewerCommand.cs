using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Application.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using ProjectModule.Shared;

namespace FeatureModule.Application.Commands;

public sealed class RemoveFeatureReviewerCommand(
    StateMachineHandler stateMachineHandler
) : ExistingFeatureCommand(stateMachineHandler)
{
    public required Guid ReviewerId { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            FeatureId != Guid.Empty
            && ReviewerId != Guid.Empty
        );

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteEvent(
            executor,
            new FeatureReviewerRemovedV1(
                FeatureReviewerId.FromDatabaseGuid(ReviewerId),
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}
