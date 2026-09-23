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

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteEvent(
            executor,
            new FeatureSkillAddedV2(
                AggregateId.FromDatabaseGuid(SkillId),
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
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

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteEvent(
            executor,
            new FeatureSkillRemovedV2(
                AggregateId.FromDatabaseGuid(SkillId),
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}
