using EventSourcing.Core;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

/// <summary>Changes the review notes or reviewers of one plan.</summary>
public abstract class FeaturePlanReviewCommand(StateMachineHandler stateMachineHandler)
    : ExistingFeatureCommand(stateMachineHandler)
{
    public required Guid PlanId { get; set; }

    protected FeaturePlanId Plan => FeaturePlanId.FromDatabaseGuid(PlanId);

    protected bool HasIds(params Guid[] ids) =>
        FeatureId != Guid.Empty && PlanId != Guid.Empty && ids.All(id => id != Guid.Empty);
}
