using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;

namespace FeatureModule.Application.Commands;

public sealed class AddFeatureSkillCommand(
    StateMachineHandler stateMachineHandler
) : ExistingFeatureCommand(stateMachineHandler)
{
    public required Guid SkillId { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            FeatureId != Guid.Empty
            && SkillId != Guid.Empty
        );

    protected override Task<object> ExecuteInternal(Executor executor) =>
        ExecuteEvent(
            executor,
            new FeatureSkillAddedV1(
                AggregateId.FromDatabaseGuid(SkillId)
            )
        );
}

public sealed class RemoveFeatureSkillCommand(
    StateMachineHandler stateMachineHandler
) : ExistingFeatureCommand(stateMachineHandler)
{
    public required Guid SkillId { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            FeatureId != Guid.Empty
            && SkillId != Guid.Empty
        );

    protected override Task<object> ExecuteInternal(Executor executor) =>
        ExecuteEvent(
            executor,
            new FeatureSkillRemovedV1(
                AggregateId.FromDatabaseGuid(SkillId)
            )
        );
}
