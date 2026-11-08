using EventSourcing.Shared.Models;

namespace ProjectModule.Domain.Constraints;

public sealed class UniqueProjectNameConstraint(ProjectStateData stateData, string normalizedName)
    : UniqueConstraint(stateData, normalizedName);
