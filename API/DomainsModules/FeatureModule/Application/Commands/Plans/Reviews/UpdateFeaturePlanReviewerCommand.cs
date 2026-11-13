using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using ProjectModule.Shared;

namespace FeatureModule.Application.Commands;

public sealed class UpdateFeaturePlanReviewerCommand(StateMachineHandler stateMachineHandler)
    : FeaturePlanReviewCommand(stateMachineHandler)
{
    public required Guid ReviewerId { get; set; }

    public required string Title { get; set; }

    public required string Description { get; set; }

    public required ReviewLevel ReviewLevel { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(HasIds(ReviewerId) && !string.IsNullOrWhiteSpace(Title)
            && !string.IsNullOrWhiteSpace(Description) && Enum.IsDefined(ReviewLevel));

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();
        return await ExecuteEvent(executor, new FeaturePlanReviewerUpdatedV1(Plan, FeatureReviewerId.FromDatabaseGuid(ReviewerId),
            Title.Trim(), Description.Trim(), ReviewLevel, SessionId, memoryAggregateId), memoryAggregateId);
    }
}
