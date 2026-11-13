using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

public sealed record DefinitionOfDoneStateInput(
    Guid Id,
    DefinitionOfDoneState State,
    string? StateDescription = null
);
