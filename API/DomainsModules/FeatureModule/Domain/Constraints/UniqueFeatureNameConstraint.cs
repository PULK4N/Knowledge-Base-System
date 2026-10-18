using EventSourcing.Shared.Models;

namespace FeatureModule.Domain.Constraints;

public sealed class UniqueFeatureNameConstraint(FeatureStateData stateData, string normalizedName)
    : UniqueConstraint(stateData, normalizedName);
