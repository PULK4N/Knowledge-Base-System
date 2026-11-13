namespace FeatureModule.Application.Commands;

public sealed record DefinitionOfDoneInput(
    string Description,
    List<Guid>? RequirementIds = null
);
