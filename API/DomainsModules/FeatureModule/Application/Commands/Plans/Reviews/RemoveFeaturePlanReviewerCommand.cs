using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

public sealed class RemoveFeaturePlanReviewerCommand(StateMachineHandler stateMachineHandler)
    : FeaturePlanReviewCommand(stateMachineHandler)
{
    public required Guid ReviewerId { get; set; }

    public override Task<bool> CanExecute(Executor executor) => Task.FromResult(HasIds(ReviewerId));

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();
        return await ExecuteEvent(executor, new FeaturePlanReviewerRemovedV1(Plan, FeatureReviewerId.FromDatabaseGuid(ReviewerId),
            SessionId, memoryAggregateId), memoryAggregateId);
    }
}
