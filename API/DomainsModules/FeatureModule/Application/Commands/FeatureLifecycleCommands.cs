using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Models;
using FeatureModule.Application.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Persistence.Interfaces;

namespace FeatureModule.Application.Commands;

public sealed class AddFeatureCommand(
    StateMachineHandler stateMachineHandler,
    IFeatureSummaryRepository featureSummaryRepository
) : FeatureCommand(stateMachineHandler)
{
    public required Guid ProjectId { get; set; }

    public required string Name { get; set; }

    public required string Summary { get; set; }

    public required string Status { get; set; }

    public override async Task<bool> CanExecute(Executor executor)
    {
        if (
            ProjectId == Guid.Empty
            || string.IsNullOrWhiteSpace(Name)
            || string.IsNullOrWhiteSpace(Status)
        )
            return false;

        return await featureSummaryRepository.GetByName(Name) is null;
    }

    protected override async Task<object> ExecuteInternal(
        Executor executor
    )
    {
        var featureId = AggregateId.New();

        await ExecuteEvent(
            executor,
            featureId,
            new FeatureAddedV1(
                AggregateId.FromDatabaseGuid(ProjectId),
                Name,
                Summary,
                Status
            )
        );

        return FeatureCreatedCommandResult.Ok(featureId.Value);
    }
}

public sealed class RemoveFeatureCommand(
    StateMachineHandler stateMachineHandler
) : ExistingFeatureCommand(stateMachineHandler)
{
    protected override Task<object> ExecuteInternal(Executor executor) =>
        ExecuteEvent(executor, new FeatureRemovedV1());
}

public sealed class UpdateFeatureStatusCommand(
    StateMachineHandler stateMachineHandler
) : ExistingFeatureCommand(stateMachineHandler)
{
    public required string Status { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            FeatureId != Guid.Empty
            && !string.IsNullOrWhiteSpace(Status)
        );

    protected override Task<object> ExecuteInternal(Executor executor) =>
        ExecuteEvent(executor, new FeatureStatusUpdatedV1(Status));
}
