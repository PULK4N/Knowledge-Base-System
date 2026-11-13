using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

public sealed class RemoveFeaturePlanReviewNoteCommand(StateMachineHandler stateMachineHandler)
    : FeaturePlanReviewCommand(stateMachineHandler)
{
    public required Guid ReviewNoteId { get; set; }

    public override Task<bool> CanExecute(Executor executor) => Task.FromResult(HasIds(ReviewNoteId));

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();
        return await ExecuteEvent(executor, new FeaturePlanReviewNoteRemovedV1(Plan,
            FeatureReviewNoteId.FromDatabaseGuid(ReviewNoteId), SessionId, memoryAggregateId), memoryAggregateId);
    }
}
