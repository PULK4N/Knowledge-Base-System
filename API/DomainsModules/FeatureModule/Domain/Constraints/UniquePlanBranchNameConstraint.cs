using EventSourcing.Shared.Models;

namespace FeatureModule.Domain.Constraints;

public sealed class UniquePlanBranchNameConstraint(FeatureStateData stateData, string projectAndBranchName)
    : UniqueConstraint(stateData, projectAndBranchName);
