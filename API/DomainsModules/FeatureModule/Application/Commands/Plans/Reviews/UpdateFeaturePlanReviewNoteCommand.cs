using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

public sealed class UpdateFeaturePlanReviewNoteCommand(StateMachineHandler stateMachineHandler)
    : FeaturePlanReviewCommand(stateMachineHandler)
{
    public required Guid ReviewNoteId { get; set; }

    public required string Title { get; set; }

    public required string Content { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(HasIds(ReviewNoteId) && !string.IsNullOrWhiteSpace(Title) && !string.IsNullOrWhiteSpace(Content));

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();
        return await ExecuteEvent(executor, new FeaturePlanReviewNoteUpdatedV1(Plan,
            FeatureReviewNoteId.FromDatabaseGuid(ReviewNoteId), Title.Trim(), Content.Trim(), SessionId, memoryAggregateId),
            memoryAggregateId);
    }
}
