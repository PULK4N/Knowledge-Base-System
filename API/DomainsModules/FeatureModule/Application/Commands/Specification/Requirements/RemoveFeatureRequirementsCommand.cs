using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Application.DTOs;
using FeatureModule.Application.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

/// <summary>Removes requirements and their definition-of-done links; their numbers are not reused.</summary>
public sealed class RemoveFeatureRequirementsCommand(StateMachineHandler stateMachineHandler)
    : FeatureSpecificationCommand(stateMachineHandler)
{
    public List<Guid> RequirementIds { get; set; } = [];

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(FeatureId != Guid.Empty && RequirementIds.Count > 0);

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();
        return await ExecuteEvent(executor, new FeatureRequirementsRemovedV1(ScopePlanId,
            [.. RequirementIds.Select(RequirementId.FromDatabaseGuid)], SessionId, memoryAggregateId), memoryAggregateId);
    }
}
