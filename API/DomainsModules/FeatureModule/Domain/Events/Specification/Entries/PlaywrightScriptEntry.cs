using System.Collections.Immutable;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public sealed record PlaywrightScriptEntry(
    PlaywrightScriptId Id,
    string Path,
    ImmutableArray<DefinitionOfDoneId> DefinitionOfDoneIds
);
