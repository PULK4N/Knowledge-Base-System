using EventSourcing.Shared.Interfaces;

namespace FeatureModule.Domain.Constraints;

public sealed class FeatureNameConstraintCreator
    : IUniqueConstraintCreator<FeatureStateData, UniqueFeatureNameConstraint>
{
    public List<UniqueFeatureNameConstraint> Create(FeatureStateData stateData)
    {
        var normalizedName = Normalize(stateData.Name);

        return normalizedName is null
            ? []
            : [new UniqueFeatureNameConstraint(stateData, normalizedName)];
    }

    private static string? Normalize(string name)
    {
        var normalizedName = name.Trim();

        return normalizedName.Length == 0
            ? null
            : normalizedName.ToUpperInvariant();
    }
}
