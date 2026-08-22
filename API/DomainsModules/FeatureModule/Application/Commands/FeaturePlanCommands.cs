using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Application.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

public sealed class AddFeaturePlanCommand(
    StateMachineHandler stateMachineHandler
) : ExistingFeatureCommand(stateMachineHandler)
{
    public required string Title { get; set; }

    public required string Content { get; set; }

    public FeaturePlanContentType ContentType { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            FeatureId != Guid.Empty
            && !string.IsNullOrWhiteSpace(Title)
            && !string.IsNullOrWhiteSpace(Content)
            && Enum.IsDefined(ContentType)
        );

    protected override async Task<object> ExecuteInternal(
        Executor executor
    )
    {
        var planId = FeaturePlanId.New();

        await ExecuteEvent(
            executor,
            new FeaturePlanAddedV1(
                planId,
                Title,
                Content,
                ContentType
            )
        );

        return FeaturePlanCreatedCommandResult.Ok(planId.Value);
    }
}

public sealed class UpdateCurrentFeaturePlanCommand(
    StateMachineHandler stateMachineHandler
) : ExistingFeatureCommand(stateMachineHandler)
{
    public required string Title { get; set; }

    public required string Content { get; set; }

    public FeaturePlanContentType ContentType { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            FeatureId != Guid.Empty
            && !string.IsNullOrWhiteSpace(Title)
            && !string.IsNullOrWhiteSpace(Content)
            && Enum.IsDefined(ContentType)
        );

    protected override Task<object> ExecuteInternal(Executor executor) =>
        ExecuteEvent(
            executor,
            new CurrentFeaturePlanUpdatedV1(
                Title,
                Content,
                ContentType
            )
        );
}

public sealed class ChangeCurrentFeaturePlanCommand(
    StateMachineHandler stateMachineHandler
) : ExistingFeatureCommand(stateMachineHandler)
{
    public required Guid PlanId { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            FeatureId != Guid.Empty
            && PlanId != Guid.Empty
        );

    protected override Task<object> ExecuteInternal(Executor executor) =>
        ExecuteEvent(
            executor,
            new CurrentFeaturePlanChangedV1(
                FeaturePlanId.FromDatabaseGuid(PlanId)
            )
        );
}

public sealed class RemoveFeaturePlanCommand(
    StateMachineHandler stateMachineHandler
) : ExistingFeatureCommand(stateMachineHandler)
{
    public required Guid PlanId { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            FeatureId != Guid.Empty
            && PlanId != Guid.Empty
        );

    protected override Task<object> ExecuteInternal(Executor executor) =>
        ExecuteEvent(
            executor,
            new FeaturePlanRemovedV1(
                FeaturePlanId.FromDatabaseGuid(PlanId)
            )
        );
}
