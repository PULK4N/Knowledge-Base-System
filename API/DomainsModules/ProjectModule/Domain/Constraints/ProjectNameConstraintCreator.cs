using EventSourcing.Shared.Interfaces;

namespace ProjectModule.Domain.Constraints;

public sealed class ProjectNameConstraintCreator
    : IUniqueConstraintCreator<ProjectStateData, UniqueProjectNameConstraint>
{
    public List<UniqueProjectNameConstraint> Create(ProjectStateData stateData)
    {
        var normalizedName = Normalize(stateData.ProjectName);

        return normalizedName is null
            ? []
            : [new UniqueProjectNameConstraint(stateData, normalizedName)];
    }

    private static string? Normalize(string name)
    {
        var normalizedName = name.Trim();

        return normalizedName.Length == 0
            ? null
            : normalizedName.ToUpperInvariant();
    }
}
