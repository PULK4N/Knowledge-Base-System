using System.Text.Json;
using EventSourcing.Shared.Interfaces;

namespace FeatureModule.Domain.Constraints;

/// <summary>
/// Reserves every plan branch name of a feature within its project, so a Git
/// branch identifies exactly one feature plan.
/// </summary>
public sealed class PlanBranchNameConstraintCreator
    : IUniqueConstraintCreator<FeatureStateData, UniquePlanBranchNameConstraint>
{
    public List<UniquePlanBranchNameConstraint> Create(FeatureStateData stateData) =>
        stateData.Plans
            .SelectMany(plan => plan.AllBranches())
            .Select(branch => branch.Name.Trim())
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Select(name => new UniquePlanBranchNameConstraint(
                stateData,
                JsonSerializer.Serialize(new List<string> { stateData.ProjectId.Value.ToString(), name })
            ))
            .ToList();
}
