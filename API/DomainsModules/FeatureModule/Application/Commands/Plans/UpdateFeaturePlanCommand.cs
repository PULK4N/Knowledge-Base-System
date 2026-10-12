using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Application.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

public sealed class UpdateFeaturePlanCommand(
    StateMachineHandler stateMachineHandler
) : FeaturePlanContentCommand(stateMachineHandler)
{
    public required Guid PlanId { get; set; }

    public override async Task<bool> CanExecute(Executor executor) =>
        PlanId != Guid.Empty && await base.CanExecute(executor);

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteEvent(
            executor,
            new FeaturePlanUpdatedV1(
                FeaturePlanId.FromDatabaseGuid(PlanId),
                Title,
                Content,
                ContentType,
                AdditionalInfo,
                Status,
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}
