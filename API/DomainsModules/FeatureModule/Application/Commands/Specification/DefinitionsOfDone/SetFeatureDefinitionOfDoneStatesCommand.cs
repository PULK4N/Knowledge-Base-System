using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Application.DTOs;
using FeatureModule.Application.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

/// <summary>Records how definitions of done were checked, or why they could not be checked yet.</summary>
public sealed class SetFeatureDefinitionOfDoneStatesCommand(StateMachineHandler stateMachineHandler)
    : FeatureSpecificationCommand(stateMachineHandler)
{
    public List<DefinitionOfDoneStateInput> States { get; set; } = [];

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(FeatureId != Guid.Empty && States.Count > 0);

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();
        return await ExecuteEvent(executor, new FeatureDefinitionsOfDoneStateSetV1(ScopePlanId,
            [.. States.Select(input => new DefinitionOfDoneStateEntry(DefinitionOfDoneId.FromDatabaseGuid(input.Id),
                input.State, input.StateDescription?.Trim() ?? string.Empty))],
            SessionId, memoryAggregateId), memoryAggregateId);
    }
}
