namespace FeatureModule.Application.Commands;

/// <summary>Null values keep the current value; an empty list removes every requirement link.</summary>
public sealed record DefinitionOfDoneChangeInput(
    Guid Id,
    string? Description = null,
    List<Guid>? RequirementIds = null
);
