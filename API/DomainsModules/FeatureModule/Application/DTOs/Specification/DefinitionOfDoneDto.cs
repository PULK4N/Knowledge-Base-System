using FeatureModule.Domain.Models;

namespace FeatureModule.Application.DTOs;

public sealed record DefinitionOfDoneDto(
    Guid Id,
    string Label,
    int Number,
    string Description,
    List<Guid> RequirementIds,
    DefinitionOfDoneState State,
    string StateDescription,
    DateTime? StateCheckedAt,
    ItemOriginDto? Origin
)
{
    public static DefinitionOfDoneDto FromModel(DefinitionOfDone definitionOfDone) =>
        new(definitionOfDone.Id.Value, SpecificationLabels.DefinitionOfDone(definitionOfDone.Number),
            definitionOfDone.Number, definitionOfDone.Description,
            definitionOfDone.RequirementIds.Select(id => id.Value).ToList(), definitionOfDone.State,
            definitionOfDone.StateDescription, definitionOfDone.StateCheckedAt,
            ItemOriginDto.FromModel(definitionOfDone.Origin));
}
