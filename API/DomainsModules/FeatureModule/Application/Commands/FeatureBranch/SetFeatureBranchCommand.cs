using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Domain.Events;

namespace FeatureModule.Application.Commands;

public sealed class SetFeatureBranchCommand(
    StateMachineHandler stateMachineHandler
) : ExistingFeatureCommand(stateMachineHandler)
{
    public required string BranchName { get; set; }

    public string Description { get; set; } = string.Empty;

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            FeatureId != Guid.Empty
            && !string.IsNullOrWhiteSpace(BranchName)
        );

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteEvent(
            executor,
            new FeatureBranchSetV1(
                BranchName.Trim(),
                Description,
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}
