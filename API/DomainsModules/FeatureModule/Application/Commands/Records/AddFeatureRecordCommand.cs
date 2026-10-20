using System.Collections.Immutable;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Interfaces;
using FeatureModule.Application.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

public sealed class AddFeatureRecordCommand(
    StateMachineHandler stateMachineHandler
) : FeatureRecordContentCommand(stateMachineHandler)
{
    /// <summary>
    /// Plans that should link the new record. The link is stored on each plan.
    /// </summary>
    public List<Guid> PlanIds { get; set; } = [];

    public override async Task<bool> CanExecute(Executor executor) =>
        PlanIds.All(planId => planId != Guid.Empty)
        && await base.CanExecute(executor);

    protected override async Task<object> ExecuteInternal(
        Executor executor
    )
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();
        var recordId = FeatureRecordId.New();
        var planIds = PlanIds
            .Distinct()
            .Select(FeaturePlanId.FromDatabaseGuid)
            .ToList();

        await ExecuteEvents(
            executor,
            new FeatureRecordAddedV3(
                recordId,
                NormalizedType,
                Title,
                Content,
                SourceType,
                SourceReference,
                SessionId,
                memoryAggregateId
            ),
            feature => planIds
                .Select(
                    planId => (IEvent)new FeaturePlanRecordsSetV1(
                        planId,
                        (feature.Plans
                            .SingleOrDefault(plan => plan.Id == planId)
                            ?.RecordIds ?? [])
                            .Append(recordId)
                            .ToImmutableArray(),
                        SessionId,
                        memoryAggregateId
                    )
                )
                .ToList(),
            memoryAggregateId
        );

        return FeatureRecordCreatedCommandResult.Ok(recordId.Value);
    }
}
