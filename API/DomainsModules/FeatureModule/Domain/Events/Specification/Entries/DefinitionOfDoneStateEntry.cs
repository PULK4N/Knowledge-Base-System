using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public sealed record DefinitionOfDoneStateEntry(
    DefinitionOfDoneId Id,
    DefinitionOfDoneState State,
    string StateDescription
);
