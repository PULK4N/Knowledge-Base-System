using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Application.DTOs;
using FeatureModule.Application.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

/// <summary>Adds definitions of done in the Not confirmed state with the next unused numbers.</summary>
public sealed class AddFeatureDefinitionsOfDoneCommand(StateMachineHandler stateMachineHandler)
    : FeatureSpecificationCommand(stateMachineHandler)
{
    public List<DefinitionOfDoneInput> DefinitionsOfDone { get; set; } = [];

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(FeatureId != Guid.Empty && DefinitionsOfDone.Count > 0);

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();
        var lastNumber = (await LoadSpecification()).LastDefinitionOfDoneNumber;
        var entries = DefinitionsOfDone
            .Select((input, index) => new DefinitionOfDoneEntry(DefinitionOfDoneId.New(), lastNumber + index + 1,
                input.Description.Trim(), [.. (input.RequirementIds ?? []).Select(RequirementId.FromDatabaseGuid)]))
            .ToList();

        await ExecuteEvent(executor,
            new FeatureDefinitionsOfDoneAddedV1(ScopePlanId, [.. entries], SessionId, memoryAggregateId), memoryAggregateId);

        return FeatureSpecificationItemsCreatedCommandResult.Ok(entries
            .Select(entry => new CreatedSpecificationItem(entry.Id.Value, SpecificationLabels.DefinitionOfDone(entry.Number)))
            .ToList());
    }
}
