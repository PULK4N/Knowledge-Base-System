using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Application.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

public abstract class FeaturePlanBranchCommand(
    StateMachineHandler stateMachineHandler
) : ExistingFeatureCommand(stateMachineHandler)
{
    public required Guid PlanId { get; set; }

    public required string BranchName { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            FeatureId != Guid.Empty
            && PlanId != Guid.Empty
            && !string.IsNullOrWhiteSpace(BranchName)
        );
}
