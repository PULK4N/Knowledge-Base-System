using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Application.DTOs;
using FeatureModule.Application.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

/// <summary>Updates script paths and definition-of-done links; omitted values are kept.</summary>
public sealed class UpdateFeatureScriptsCommand(StateMachineHandler stateMachineHandler)
    : FeatureSpecificationCommand(stateMachineHandler)
{
    public List<ScriptChangeInput> Scripts { get; set; } = [];

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(FeatureId != Guid.Empty && Scripts.Count > 0);

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();
        var current = (await LoadSpecification()).Scripts.ToDictionary(item => item.Id);
        var entries = Scripts
            .Select(input =>
            {
                var id = ScriptId.FromDatabaseGuid(input.Id);
                var existing = current.GetValueOrDefault(id) ?? new Script { Id = id };
                return new ScriptEntry(id, input.Path?.Trim() ?? existing.Path,
                    input.DefinitionOfDoneIds is null
                        ? [.. existing.DefinitionOfDoneIds]
                        : [.. input.DefinitionOfDoneIds.Select(DefinitionOfDoneId.FromDatabaseGuid)]);
            })
            .ToList();

        return await ExecuteEvent(executor,
            new FeatureScriptsUpdatedV1(ScopePlanId, [.. entries], SessionId, memoryAggregateId), memoryAggregateId);
    }
}
