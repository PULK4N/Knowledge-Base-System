namespace FeatureModule.Domain.Models;

/// <summary>
/// The plan item a feature-level copy was made from when its plan completed.
/// The copy has its own ID; this points back to the original on the plan.
/// </summary>
public sealed record ItemOrigin(FeaturePlanId PlanId, Guid PlanItemId);
