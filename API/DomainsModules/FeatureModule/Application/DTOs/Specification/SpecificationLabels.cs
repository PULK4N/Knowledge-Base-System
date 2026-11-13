using FeatureModule.Domain.Models;

namespace FeatureModule.Application.DTOs;

/// <summary>The readable labels shown for specification items, such as FR-3 and DOD-2.</summary>
public static class SpecificationLabels
{
    public static string Requirement(RequirementType type, int number) =>
        type switch
        {
            RequirementType.Business => "BR",
            RequirementType.Functional => "FR",
            RequirementType.NonFunctional => "NFR",
            RequirementType.Technical => "TR",
            _ => type.ToString()
        } + "-" + number;

    public static string DefinitionOfDone(int number) => "DOD-" + number;
}
