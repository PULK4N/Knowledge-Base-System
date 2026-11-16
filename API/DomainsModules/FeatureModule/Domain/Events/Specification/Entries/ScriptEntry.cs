using System.Collections.Immutable;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public sealed record ScriptEntry(
    ScriptId Id,
    string Path,
    ImmutableArray<DefinitionOfDoneId> DefinitionOfDoneIds
);
