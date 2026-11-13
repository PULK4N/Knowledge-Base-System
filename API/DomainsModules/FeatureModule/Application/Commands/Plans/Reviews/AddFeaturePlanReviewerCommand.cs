using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Application.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using ProjectModule.Shared;

namespace FeatureModule.Application.Commands;

public sealed class AddFeaturePlanReviewerCommand(StateMachineHandler stateMachineHandler)
    : FeaturePlanReviewCommand(stateMachineHandler)
{
    public required string Title { get; set; }

    public required string Description { get; set; }

    public required ReviewLevel ReviewLevel { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(HasIds() && !string.IsNullOrWhiteSpace(Title) && !string.IsNullOrWhiteSpace(Description)
            && Enum.IsDefined(ReviewLevel));

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();
        var reviewerId = FeatureReviewerId.New();
        await ExecuteEvent(executor, new FeaturePlanReviewerAddedV1(Plan, reviewerId, Title.Trim(), Description.Trim(),
            ReviewLevel, SessionId, memoryAggregateId), memoryAggregateId);
        return FeatureReviewerCreatedCommandResult.Ok(reviewerId.Value);
    }
}
