using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Application.DTOs;
using FeatureModule.Application.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

/// <summary>Updates definition-of-done descriptions and requirement links; omitted values are kept.</summary>
public sealed class UpdateFeatureDefinitionsOfDoneCommand(StateMachineHandler stateMachineHandler)
    : FeatureSpecificationCommand(stateMachineHandler)
{
    public List<DefinitionOfDoneChangeInput> DefinitionsOfDone { get; set; } = [];

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(FeatureId != Guid.Empty && DefinitionsOfDone.Count > 0);

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();
        var current = (await LoadSpecification()).DefinitionsOfDone.ToDictionary(item => item.Id);
        var changes = DefinitionsOfDone
            .Select(input =>
            {
                var id = DefinitionOfDoneId.FromDatabaseGuid(input.Id);
                var existing = current.GetValueOrDefault(id) ?? new DefinitionOfDone { Id = id };
                return new DefinitionOfDoneChange(id, input.Description?.Trim() ?? existing.Description,
                    input.RequirementIds is null
                        ? [.. existing.RequirementIds]
                        : [.. input.RequirementIds.Select(RequirementId.FromDatabaseGuid)]);
            })
            .ToList();

        return await ExecuteEvent(executor,
            new FeatureDefinitionsOfDoneUpdatedV1(ScopePlanId, [.. changes], SessionId, memoryAggregateId), memoryAggregateId);
    }
}
