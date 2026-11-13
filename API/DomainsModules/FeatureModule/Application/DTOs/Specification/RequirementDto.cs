using FeatureModule.Domain.Models;

namespace FeatureModule.Application.DTOs;

public sealed record RequirementDto(
    Guid Id,
    string Label,
    RequirementType Type,
    int Number,
    string Description,
    string ExternalId,
    string Source,
    ItemOriginDto? Origin
)
{
    public static RequirementDto FromModel(Requirement requirement) =>
        new(requirement.Id.Value, SpecificationLabels.Requirement(requirement.Type, requirement.Number),
            requirement.Type, requirement.Number, requirement.Description, requirement.ExternalId,
            requirement.Source, ItemOriginDto.FromModel(requirement.Origin));
}
