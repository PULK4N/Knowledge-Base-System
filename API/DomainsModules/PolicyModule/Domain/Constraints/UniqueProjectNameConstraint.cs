using EventSourcing.Shared.Models;

namespace PolicyModule.Domain.Constraints;

public sealed class UniqueProjectNameConstraint(ProjectPoliciesStateData stateData, string normalizedName)
    : UniqueConstraint(stateData, normalizedName);
