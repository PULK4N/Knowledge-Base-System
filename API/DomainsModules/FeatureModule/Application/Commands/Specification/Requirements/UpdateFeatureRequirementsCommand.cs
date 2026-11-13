using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Application.DTOs;
using FeatureModule.Application.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

/// <summary>
/// Updates requirements; omitted values are kept. A requirement whose type
/// changes gets the next unused number of the new type.
/// </summary>
public sealed class UpdateFeatureRequirementsCommand(StateMachineHandler stateMachineHandler)
    : FeatureSpecificationCommand(stateMachineHandler)
{
    public List<RequirementChangeInput> Requirements { get; set; } = [];

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(FeatureId != Guid.Empty && Requirements.Count > 0);

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();
        var specification = await LoadSpecification();
        var current = specification.Requirements.ToDictionary(item => item.Id);
        var nextNumbers = new Dictionary<RequirementType, int>();
        var entries = Requirements
            .Select(input =>
            {
                var id = RequirementId.FromDatabaseGuid(input.Id);
                var existing = current.GetValueOrDefault(id) ?? new Requirement { Id = id };
                var type = input.Type ?? existing.Type;
                var number = type == existing.Type && current.ContainsKey(id)
                    ? existing.Number
                    : nextNumbers[type] = nextNumbers.GetValueOrDefault(type, specification.LastRequirementNumber(type)) + 1;
                return new RequirementEntry(id, type, number,
                    input.Description?.Trim() ?? existing.Description,
                    input.ExternalId?.Trim() ?? existing.ExternalId,
                    input.Source?.Trim() ?? existing.Source);
            })
            .ToList();

        return await ExecuteEvent(executor,
            new FeatureRequirementsUpdatedV1(ScopePlanId, [.. entries], SessionId, memoryAggregateId), memoryAggregateId);
    }
}
