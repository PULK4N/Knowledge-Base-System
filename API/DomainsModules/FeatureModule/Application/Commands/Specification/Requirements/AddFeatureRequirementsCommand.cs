using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Application.DTOs;
using FeatureModule.Application.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

/// <summary>Adds requirements; each gets the next unused number of its type.</summary>
public sealed class AddFeatureRequirementsCommand(StateMachineHandler stateMachineHandler)
    : FeatureSpecificationCommand(stateMachineHandler)
{
    public List<RequirementInput> Requirements { get; set; } = [];

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(FeatureId != Guid.Empty && Requirements.Count > 0);

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();
        var specification = await LoadSpecification();
        var nextNumbers = new Dictionary<RequirementType, int>();
        var entries = Requirements
            .Select(input =>
            {
                var number = nextNumbers[input.Type] =
                    nextNumbers.GetValueOrDefault(input.Type, specification.LastRequirementNumber(input.Type)) + 1;
                return new RequirementEntry(RequirementId.New(), input.Type, number, input.Description.Trim(),
                    input.ExternalId?.Trim() ?? string.Empty, input.Source?.Trim() ?? string.Empty);
            })
            .ToList();

        await ExecuteEvent(executor,
            new FeatureRequirementsAddedV1(ScopePlanId, [.. entries], SessionId, memoryAggregateId), memoryAggregateId);

        return FeatureSpecificationItemsCreatedCommandResult.Ok(entries
            .Select(entry => new CreatedSpecificationItem(entry.Id.Value, SpecificationLabels.Requirement(entry.Type, entry.Number)))
            .ToList());
    }
}
