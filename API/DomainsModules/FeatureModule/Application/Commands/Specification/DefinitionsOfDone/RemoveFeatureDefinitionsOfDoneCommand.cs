using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Application.DTOs;
using FeatureModule.Application.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

/// <summary>Removes definitions of done and their script links; their numbers are not reused.</summary>
public sealed class RemoveFeatureDefinitionsOfDoneCommand(StateMachineHandler stateMachineHandler)
    : FeatureSpecificationCommand(stateMachineHandler)
{
    public List<Guid> DefinitionOfDoneIds { get; set; } = [];

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(FeatureId != Guid.Empty && DefinitionOfDoneIds.Count > 0);

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();
        return await ExecuteEvent(executor, new FeatureDefinitionsOfDoneRemovedV1(ScopePlanId,
            [.. DefinitionOfDoneIds.Select(DefinitionOfDoneId.FromDatabaseGuid)], SessionId, memoryAggregateId), memoryAggregateId);
    }
}
