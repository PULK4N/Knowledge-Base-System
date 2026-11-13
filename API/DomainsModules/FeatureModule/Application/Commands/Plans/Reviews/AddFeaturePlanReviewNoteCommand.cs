using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Application.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

public sealed class AddFeaturePlanReviewNoteCommand(StateMachineHandler stateMachineHandler)
    : FeaturePlanReviewCommand(stateMachineHandler)
{
    public required string Title { get; set; }

    public required string Content { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(HasIds() && !string.IsNullOrWhiteSpace(Title) && !string.IsNullOrWhiteSpace(Content));

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();
        var reviewNoteId = FeatureReviewNoteId.New();
        await ExecuteEvent(executor, new FeaturePlanReviewNoteAddedV1(Plan, reviewNoteId, Title.Trim(), Content.Trim(),
            SessionId, memoryAggregateId), memoryAggregateId);
        return FeatureReviewNoteCreatedCommandResult.Ok(reviewNoteId.Value);
    }
}
