using ActionModule.Shared.Models;
using EventSourcing.Core;

namespace FeatureModule.Application.Commands;

public sealed class SetFeatureCompositeBranchCommand(StateMachineHandler stateMachineHandler)
    : ExistingFeatureCommand(stateMachineHandler)
{
    /// <summary>
    /// Empty clears the composite testing branch.
    /// </summary>
    public required string BranchName { get; set; }

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();
        return await ExecuteEvent(
            executor,
            FeatureCompositeBranchEvents.Create(BranchName, SessionId, memoryAggregateId),
            memoryAggregateId
        );
    }
}
