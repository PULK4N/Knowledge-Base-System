using EventSourcing.Core;
using EventSourcing.Shared.Models;
using FeatureModule.Domain;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

/// <summary>
/// Changes the specification of a plan, or of the feature when PlanId is null.
/// </summary>
public abstract class FeatureSpecificationCommand(StateMachineHandler stateMachineHandler)
    : ExistingFeatureCommand(stateMachineHandler)
{
    public Guid? PlanId { get; set; }

    protected FeaturePlanId? ScopePlanId =>
        PlanId is { } planId && planId != Guid.Empty ? FeaturePlanId.FromDatabaseGuid(planId) : null;

    /// <summary>
    /// The targeted specification as it is now, used to assign numbers and keep
    /// unchanged values. An empty one when the feature or plan is missing, so
    /// the validators report it.
    /// </summary>
    protected async Task<Specification> LoadSpecification() =>
        (await LoadFeature())?.FindSpecification(ScopePlanId) ?? new Specification();

    protected async Task<FeatureStateData?> LoadFeature() =>
        (await stateMachineHandler.GetByAggregateId(AggregateId.FromDatabaseGuid(FeatureId)))?.StateData
            as FeatureStateData;
}
