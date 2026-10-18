using EventSourcing.Shared.Models;

namespace SkillsModule.Domain.Constraints;

public sealed class UniqueSkillNameConstraint(SkillStateData stateData, string normalizedName)
    : UniqueConstraint(stateData, normalizedName);
